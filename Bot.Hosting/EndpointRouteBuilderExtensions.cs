using Bot.Core.Stats;
using Bot.Hosting.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

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
        endpoints.MapGet(
            "/admin/stats",
            (StatsCollector stats, HttpRequest request, IOptions<BotOptions> options) =>
            {
                if (!request.Headers.TryGetValue("X-Admin-Token", out var token) ||
                    token != options.Value.AdminToken)
                {
                    return Results.StatusCode(StatusCodes.Status401Unauthorized);
                }

                return Results.Json(stats.GetSnapshot());
            });
        return endpoints;
    }
}
