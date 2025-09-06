using Bot.Abstractions.Contracts;
using Bot.Core.Options;
using Bot.Core.Transport;
using Bot.Hosting.Options;
using Bot.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Telegram.Bot;

namespace Bot.Telegram;

/// <summary>
///     Расширения <see cref="IServiceCollection" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Регистрирует HTTP-клиент и клиента Telegram.</item>
///         <item>Настраивает источники обновлений.</item>
///         <item>Подключает транспорт, валидатор и ответчик Web App.</item>
///     </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Добавить телегу
    /// </summary>
    public static IServiceCollection AddTelegramTransport(this IServiceCollection services)
    {
        const string telegram = "telegram";
        services.AddHttpClient(telegram);
        services.AddMemoryCache();
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
            return new TelegramBotClient(opts.Token,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(telegram));
        });
        services.AddOptions<QueueOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QueueOptions>>().Value);
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
        services.AddSingleton<IMessageKeyProvider, GuidMessageKeyProvider>();
        services.AddSingleton<TelegramTransportClient>();
        services.AddSingleton<ITransportClient>(sp =>
            new OutboxTransportClient(sp.GetRequiredService<TelegramTransportClient>(),
                sp.GetRequiredService<IOutbox>(),
                sp.GetRequiredService<IMessageKeyProvider>()));
        services.AddSingleton<IBotUi, TelegramBotUi>();
        services.AddSingleton<IChatMenuService, ChatMenuService>();
        return services;
    }
}
