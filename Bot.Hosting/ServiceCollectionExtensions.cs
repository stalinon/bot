using System.Reflection;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Routing;
using Bot.Hosting.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Bot.Hosting;

/// <summary>
///     Расширения <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавить бота
    /// </summary>
    public static IServiceCollection AddBot(this IServiceCollection services, Action<BotOptions> configure)
    {
        services.AddOptions<BotOptions>().Configure(configure);
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<RateLimitOptions>(sp => sp
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BotOptions>>().Value.RateLimits);
        services.AddSingleton<BotHostedService>();
        services.AddHostedService<BotHostedService>();
        services.AddMetrics();
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
    ///     Использовать хранилище состояний
    /// </summary>
    public static IServiceCollection UseStateStore(this IServiceCollection services, IStateStore store)
    {
        services.AddSingleton(store);
        return services;
    }
}