using Bot.Core.Stats;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bot.Hosting;

/// <summary>
///     Расширения для маршрутизации служебных эндпоинтов бота.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Подключить health-пробы.
    /// </summary>
    public static IEndpointRouteBuilder MapBotHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok());
        endpoints.MapGet("/health/ready", () => Results.Ok());
        return endpoints;
    }

    /// <summary>
    ///     Подключить эндпоинт статистики.
    /// </summary>
    public static IEndpointRouteBuilder MapBotStats(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/stats", (StatsCollector stats) => Results.Json(stats.GetSnapshot()));
        return endpoints;
    }
}
