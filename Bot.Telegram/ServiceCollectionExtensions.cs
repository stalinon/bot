using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Bot.Telegram;

/// <summary>
///     Расширения <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавить телегу
    /// </summary>
    public static IServiceCollection AddTelegramTransport(this IServiceCollection services)
    {
        const string telegram = "telegram";
        services.AddHttpClient(telegram);
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
            return new TelegramBotClient(opts.Token, sp.GetRequiredService<IHttpClientFactory>().CreateClient(telegram));
        });
          services.AddSingleton<TelegramPollingSource>();
          services.AddSingleton<TelegramWebhookSource>();
          services.AddSingleton<WebhookService>();
        services.AddSingleton<IUpdateSource>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
            return opts.Transport.Mode == TransportMode.Webhook
                ? sp.GetRequiredService<TelegramWebhookSource>()
                : sp.GetRequiredService<TelegramPollingSource>();
        });
        services.AddSingleton<ITransportClient, TelegramTransportClient>();
        return services;
    }
}