using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Bot.Hosting.Options;

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
        endpoints.MapPost($"/tg/{opts.Transport.Secret}", async (Update update, TelegramWebhookSource source) =>
        {
            await source.Enqueue(update);
            return Results.Ok();
        });
        return endpoints;
    }
}
