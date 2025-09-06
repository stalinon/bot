using Bot.Hosting.Options;
using Bot.Observability;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Telegram.Bot.Types;

namespace Bot.Telegram;

/// <summary>
///     Расширения для маршрутизации телеграм вебхука
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Подключить эндпоинт телеграм вебхука
    /// </summary>
    public static IEndpointRouteBuilder MapTelegramWebhook(this IEndpointRouteBuilder endpoints)
    {
        var opts = endpoints.ServiceProvider.GetRequiredService<IOptions<BotOptions>>().Value;
        endpoints.MapPost(
            $"/tg/{opts.Transport.Webhook.Secret}",
            async (Update update, TelegramWebhookSource source, ILogger<TelegramWebhookSource> logger, CancellationToken ct) =>
            {
                using var activity = Telemetry.ActivitySource.StartActivity("Webhook/Ingress");
                activity?.SetTag("transport", "telegram");
                activity?.SetTag("update.id", update.Id);
                if (!await source.TryEnqueueAsync(update, ct))
                {
                    logger.LogWarning("webhook queue overflow for update {UpdateId}", update.Id);
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);
                }

                return Results.Ok();
            });
        return endpoints;
    }
}
