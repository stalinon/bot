using System.Reflection;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Routing;
using Bot.Core.Utils;
using Bot.Core.Stats;
using Bot.Hosting.Options;
using Bot.Storage.EFCore;
using Bot.Storage.File;
using Bot.Storage.File.Options;
using Bot.Storage.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using StackExchange.Redis;

namespace Bot.Hosting;

/// <summary>
///     Расширения <see cref="IServiceCollection"/>.
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
    /// <param name="metrics">Дополнительная настройка <see cref="MeterProviderBuilder"/>.</param>
    public static IServiceCollection AddBot(this IServiceCollection services, Action<BotOptions> configure, Action<MeterProviderBuilder>? metrics = null)
    {
        services.AddOptions<BotOptions>().Configure(configure);
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<RateLimitOptions>(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BotOptions>>()
                .Value.RateLimits);
        services.AddSingleton(sp => new TtlCache<string>(sp
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BotOptions>>()
            .Value.DeduplicationTtl));
        services.AddSingleton<BotHostedService>();
        services.AddHostedService<BotHostedService>();
        services.AddMetrics();
        services.AddSingleton<StatsCollector>();

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
    public static IServiceCollection UsePipeline(this IServiceCollection services, Action<IUpdatePipeline> use)
    {
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BotHostedService>());
        services.AddSingleton(use);
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
    ///     Использовать хранилище состояний.
    /// </summary>
    public static IServiceCollection UseStateStorage(this IServiceCollection services, IStateStore store)
    {
        services.AddSingleton<IStateStore>(store);
        services.AddSingleton<IStateStorage>(sp => sp.GetRequiredService<IStateStore>());
        return services;
    }

    /// <summary>
    ///     Использовать хранилище состояний из конфигурации.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    public static IServiceCollection UseConfiguredStateStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["STORAGE:PROVIDER"] ?? "file").ToLowerInvariant();
        switch (provider)
        {
            case "redis":
                var conn = configuration["STORAGE:REDIS:CONNECTION"] ?? "localhost";
                var db = int.TryParse(configuration["STORAGE:REDIS:DB"], out var d) ? d : 0;
                var prefix = configuration["STORAGE:REDIS:PREFIX"] ?? string.Empty;
                var mux = ConnectionMultiplexer.Connect(conn);
                var options = new RedisOptions
                {
                    Connection = mux,
                    Database = db,
                    Prefix = prefix,
                };
                services.UseStateStorage(new RedisStateStore(options));
                break;
            case "ef":
                var cs = configuration["STORAGE:EF:CONNECTION"] ?? "Data Source=bot_state.db";
                services.AddDbContext<StateContext>(o => o.UseSqlite(cs));
                services.AddScoped<IStateStore, EfCoreStateStore>();
                services.AddScoped<IStateStorage>(sp => (IStateStorage)sp.GetRequiredService<IStateStore>());
                break;
            default:
                var path = configuration["STORAGE:FILE:PATH"] ?? "data";
                services.UseStateStorage(new FileStateStore(new FileStoreOptions { Path = path }));
                break;
        }

        return services;
    }
}