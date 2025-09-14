using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using OpenTelemetry.Metrics;

using StackExchange.Redis;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Pipeline;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Hosting.Options;
using Stalinon.Bot.Logging;
using Stalinon.Bot.Observability;
using Stalinon.Bot.Outbox;
using Stalinon.Bot.Scheduler;
using Stalinon.Bot.Storage.EFCore;
using Stalinon.Bot.Storage.File;
using Stalinon.Bot.Storage.File.Options;
using Stalinon.Bot.Storage.Redis;
using Stalinon.Bot.WebApp.MinimalApi;

namespace Stalinon.Bot.Hosting;

/// <summary>
///     Расширения <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Подключают бота и необходимые сервисы</item>
///         <item>Настраивают хранилище состояний</item>
///     </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавить бота.
    /// </summary>
    /// <param name="configure">Настройка параметров бота.</param>
    /// <param name="metrics">Дополнительная настройка <see cref="MeterProviderBuilder" />.</param>
    public static IServiceCollection AddBot(this IServiceCollection services, Action<BotOptions> configure,
        Action<MeterProviderBuilder>? metrics = null)
    {
        services.AddLogging(b => b.AddBotLogging());
        services.AddOptions<BotOptions>().Configure(configure);
        services.AddOptions<QueueOptions>().BindConfiguration("Queue");
        services.AddOptions<StopOptions>().BindConfiguration("Stop");
        services.AddOptions<ObservabilityOptions>().BindConfiguration("Obs");
        services.AddOptions<OutboxOptions>().BindConfiguration("Outbox");
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<QueueOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<StopOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OutboxOptions>>().Value);
        services.AddSingleton<IOutbox>(sp => new FileOutbox(sp.GetRequiredService<IOptions<OutboxOptions>>().Value.Path));
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<RateLimitOptions>(sp =>
            sp.GetRequiredService<IOptions<BotOptions>>()
                .Value.RateLimits);
        services.AddSingleton<DeduplicationOptions>(sp =>
            sp.GetRequiredService<IOptions<BotOptions>>()
                .Value.Deduplication);
        services.AddSingleton<BotHostedService>();
        services.AddHostedService<BotHostedService>();
        services.AddMetrics();
        services.AddSingleton<StatsCollector>();
        services.AddSingleton<WebAppStatsCollector>();
        services.AddSingleton<CustomStats>();

        if (metrics is not null)
        {
            services.AddOpenTelemetry().WithMetrics(builder =>
            {
                builder.AddMeter(MetricsMiddleware.MeterName);
                metrics(builder);
            });
        }

        return services;
    }

    /// <summary>
    ///     Добавить пайплайн
    /// </summary>
    public static IServiceCollection UsePipeline(this IServiceCollection services)
    {
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        return services;
    }

    /// <summary>
    ///     Добавить обработчики
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var registryConfigured = new HandlerRegistry();
        registryConfigured.RegisterFrom(assembly);
        foreach (var t in assembly.GetTypes())
        {
            if (t is not { IsAbstract: false, IsInterface: false })
            {
                continue;
            }

            if (typeof(IFallbackHandler).IsAssignableFrom(t))
            {
                services.TryAddTransient(typeof(IFallbackHandler), t);
                continue;
            }

            if (typeof(IUpdateHandler).IsAssignableFrom(t))
            {
                services.TryAddTransient(t);
            }
        }

        services.AddSingleton(registryConfigured);

        services.AddScoped<IUpdateMiddleware, ExceptionHandlingMiddleware>()
            .AddScoped<IUpdateMiddleware, MetricsMiddleware>()
            .AddScoped<IUpdateMiddleware, LoggingMiddleware>()
            .AddScoped<IUpdateMiddleware, DedupMiddleware>()
            .AddScoped<IUpdateMiddleware, RateLimitMiddleware>()
            .AddScoped<IUpdateMiddleware, CommandParsingMiddleware>()
            .AddScoped<IUpdateMiddleware, RouterMiddleware>();

        return services;
    }

    /// <summary>
    ///     Использовать распределённые локи.
    /// </summary>
    /// <param name="provider">Провайдер локов.</param>
    public static IServiceCollection UseDistributedLock(this IServiceCollection services, IDistributedLock provider)
    {
        services.AddSingleton(provider);
        return services;
    }

    /// <summary>
    ///     Использовать хранилище состояний.
    /// </summary>
    public static IServiceCollection UseStateStorage(this IServiceCollection services, IStateStore store)
    {
        services.AddSingleton<IStateStore>(new TracingStateStore(store));
        services.AddSingleton<IStateStorage>(sp => sp.GetRequiredService<IStateStore>());
        return services;
    }

    /// <summary>
    ///     Использовать хранилище состояний из конфигурации.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public static IServiceCollection UseConfiguredStateStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage");
        var provider = section == null ? "file" : (section["Provider"] ?? "file").ToLowerInvariant();
        switch (provider)
        {
            case "redis":
                var redis = section.GetSection("Redis");
                var conn = redis["Connection"] ?? "localhost";
                var db = int.TryParse(redis["Db"], out var d) ? d : 0;
                var prefix = redis["Prefix"] ?? string.Empty;
                var mux = ConnectionMultiplexer.Connect(conn);
                var options = new RedisOptions
                {
                    Connection = mux,
                    Database = db,
                    Prefix = prefix
                };
                services.UseStateStorage(new RedisStateStore(options));
                break;
            case "ef":
                var ef = section.GetSection("Ef");
                var cs = ef["Connection"] ?? "Data Source=bot_state.db";
                var efProvider = (ef["Provider"] ?? "sqlite").ToLowerInvariant();
                services.AddDbContext<StateContext>(o =>
                {
                    if (efProvider == "postgres")
                    {
                        o.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName));
                    }
                    else
                    {
                        o.UseSqlite(cs, b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName));
                    }
                });
                services.AddScoped<IStateStore>(sp =>
                    new TracingStateStore(new EfCoreStateStore(sp.GetRequiredService<StateContext>())));
                services.AddScoped<IStateStorage>(sp => sp.GetRequiredService<IStateStore>());
                break;
            default:
                if (section == null)
                {
                    services.UseStateStorage(new FileStateStore(new FileStoreOptions { Path = "data" }));
                    return services;
                }

                var file = section.GetSection("File");
                var path = file["Path"] ?? "data";
                services.UseStateStorage(new FileStateStore(new FileStoreOptions { Path = path }));
                break;
        }

        return services;
    }

    /// <summary>
    ///     Добавить планировщик задач.
    /// </summary>
    public static IServiceCollection AddJobScheduler(this IServiceCollection services)
    {
        services.AddHostedService<JobScheduler>();
        return services;
    }

    /// <summary>
    ///     Зарегистрировать задачу.
    /// </summary>
    /// <param name="cron">Cron-выражение.</param>
    /// <param name="interval">Интервал выполнения.</param>
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services, string? cron = null,
        TimeSpan? interval = null)
        where TJob : class, IJob
    {
        services.TryAddTransient<TJob>();
        services.AddSingleton(new JobDescriptor(typeof(TJob), cron, interval));
        return services;
    }

    /// <summary>
    ///     Подключить Mini App.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    public static IServiceCollection AddWebApp(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("WebApp");
        services.AddOptions<WebAppOptions>().Bind(section);
        services.AddOptions<WebAppAuthOptions>().Configure(o =>
        {
            o.Secret = section["Secret"] ?? string.Empty;
            var authTtl = section.GetValue("AuthTtlSeconds", 300);
            o.Lifetime = TimeSpan.FromSeconds(authTtl);
        });

        var validatorType = Type.GetType("Stalinon.Bot.Telegram.WebAppInitDataValidator, Stalinon.Bot.Telegram");
        if (validatorType is not null)
        {
            services.TryAddSingleton(typeof(IWebAppInitDataValidator), validatorType);
        }

        var responderType = Type.GetType("Stalinon.Bot.Telegram.TelegramWebAppQueryResponder, Stalinon.Bot.Telegram");
        if (responderType is not null)
        {
            services.TryAddSingleton(typeof(IWebAppQueryResponder), sp =>
            {
                var ttl = TimeSpan.FromSeconds(
                    sp.GetRequiredService<IOptions<WebAppOptions>>().Value.InitDataTtlSeconds);
                return ActivatorUtilities.CreateInstance(sp, responderType, ttl);
            });
        }

        return services;
    }
}
