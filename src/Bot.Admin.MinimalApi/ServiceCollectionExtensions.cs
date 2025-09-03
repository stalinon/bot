using Bot.Core.Stats;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Admin.MinimalApi;

/// <summary>
///     Расширения для регистрации служб административного API.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавить службы административного API.
    /// </summary>
    public static IServiceCollection AddAdminApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<StatsCollector>();
        services.Configure<AdminOptions>(configuration.GetSection("Admin"));
        services.AddSingleton<IHealthProbe, TransportProbe>();
        services.AddSingleton<IHealthProbe, QueueProbe>();
        services.AddSingleton<IHealthProbe, StorageProbe>();
        return services;
    }
}

