using System.Reflection;

using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Routing;
using Bot.Core.Stats;
using Bot.Hosting.Options;
using Bot.Logging;
using Bot.Observability;
using Bot.Scheduler;
using Bot.Storage.EFCore;
using Bot.Storage.File;
using Bot.Storage.File.Options;
using Bot.Storage.Redis;
using Bot.WebApp.MinimalApi;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using OpenTelemetry.Metrics;

using StackExchange.Redis;

namespace Bot.Hosting;

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

        services.AddScoped<ExceptionHandlingMiddleware>()
            .AddScoped<MetricsMiddleware>()
            .AddScoped<LoggingMiddleware>()
            .AddScoped<DedupMiddleware>()
            .AddScoped<RateLimitMiddleware>()
            .AddScoped<CommandParsingMiddleware>()
            .AddScoped<RouterMiddleware>();

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
    public static IServiceCollection UseConfiguredStateStorage(this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage");
        var provider = (section["Provider"] ?? "file").ToLowerInvariant();
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

        var validatorType = Type.GetType("Bot.Telegram.WebAppInitDataValidator, Bot.Telegram");
        if (validatorType is not null)
        {
            services.TryAddSingleton(typeof(IWebAppInitDataValidator), validatorType);
        }

        var responderType = Type.GetType("Bot.Telegram.TelegramWebAppQueryResponder, Bot.Telegram");
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
