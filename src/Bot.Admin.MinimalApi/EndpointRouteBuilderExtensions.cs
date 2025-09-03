using Bot.Core.Stats;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Bot.Admin.MinimalApi;

/// <summary>
///     Расширения для подключения административных эндпоинтов.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Подключить все административные эндпоинты.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAdminHealth();
        endpoints.MapAdminStats();
        endpoints.MapAdminBroadcast();
        return endpoints;
    }

    /// <summary>
    ///     Подключить health-пробы.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok());
        endpoints.MapGet("/health/ready", async (IEnumerable<IHealthProbe> probes, CancellationToken ct) =>
        {
            foreach (var probe in probes)
            {
                try
                {
                    await probe.ProbeAsync(ct);
                }
                catch
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            }

            return Results.Ok();
        });
        return endpoints;
    }

    /// <summary>
    ///     Подключить эндпоинт статистики.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminStats(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/stats", (StatsCollector stats, HttpRequest req, IOptions<AdminOptions> options) =>
        {
            if (!req.Headers.TryGetValue("X-Admin-Token", out var token) ||
                token != options.Value.AdminToken)
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            return Results.Json(stats.GetSnapshot());
        });
        return endpoints;
    }

    /// <summary>
    ///     Подключить эндпоинт рассылки.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminBroadcast(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/admin/broadcast", (
            BroadcastRequest request,
            HttpRequest http,
            IOptions<AdminOptions> options,
            ILogger<BroadcastRequest> logger) =>
        {
            if (!http.Headers.TryGetValue("X-Admin-Token", out var token) ||
                token != options.Value.AdminToken)
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            foreach (var chatId in request.ChatIds)
            {
                try
                {
                    logger.LogInformation("Sent to {ChatId}", chatId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send to {ChatId}", chatId);
                }
            }

            return Results.Ok();
        });
        return endpoints;
    }
}

