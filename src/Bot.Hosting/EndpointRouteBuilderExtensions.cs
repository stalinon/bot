using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading.Channels;

namespace Bot.Hosting;
/// <summary>
///     Расширения для подключения проб готовности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Подключает эндпоинт готовности.</item>
///         <item>Проверяет зависимости и очередь.</item>
///     </list>
/// </remarks>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Подключить эндпоинты проверки готовности.
    /// </summary>
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/ready", async (
            IUpdateSource source,
            IStateStore storage,
            BotHostedService hosted,
            IOptions<BotOptions> options,
            CancellationToken ct) =>
        {
            try
            {
                _ = source;
                var token = Guid.NewGuid().ToString("N");
                await storage.SetAsync("health", token, 1, TimeSpan.FromSeconds(1), ct);
                await storage.RemoveAsync("health", token, ct);

                var channelField = typeof(BotHostedService)
                    .GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance);
                var channel = channelField?.GetValue(hosted) as Channel<UpdateContext>;
                if (channel is null)
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                var capacity = options.Value.Transport.Parallelism * 16;
                if (channel.Reader.Count >= capacity)
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Ok();
        });
        return endpoints;
    }
}

