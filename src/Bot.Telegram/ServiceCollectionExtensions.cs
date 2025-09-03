using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Bot.Telegram;

/// <summary>
///     Расширения <see cref="IServiceCollection"/>
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
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
            return new TelegramBotClient(opts.Token, sp.GetRequiredService<IHttpClientFactory>().CreateClient(telegram));
        });
        services.AddSingleton<TelegramPollingSource>();
        services.AddSingleton<TelegramWebhookSource>();
        services.AddSingleton<WebhookService>();
        services.AddSingleton<IWebAppInitDataValidator, WebAppInitDataValidator>();
        services.AddSingleton<IUpdateSource>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
            return opts.Transport.Mode == TransportMode.Webhook
                ? sp.GetRequiredService<TelegramWebhookSource>()
                : sp.GetRequiredService<TelegramPollingSource>();
        });
        services.AddSingleton<ITransportClient, TelegramTransportClient>();
        services.AddSingleton<IWebAppQueryResponder, TelegramWebAppQueryResponder>();
        services.AddSingleton<IBotUi, TelegramBotUi>();
        services.AddSingleton<IChatMenuService, ChatMenuService>();
        return services;
    }
}
