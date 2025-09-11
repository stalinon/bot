using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Telegram.Bot;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Examples.WebhookBot.Services;
using Stalinon.Bot.TestKit;

namespace Stalinon.Bot.Examples.WebhookBot.Tests.Integration;

/// <summary>
///     Фабрика тестового приложения WebhookBot.
/// </summary>
public sealed class WebhookBotFactory : WebApplicationFactory<RequestIdProvider>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var path = Path.Combine(root, "examples", "Stalinon.Bot.Examples.WebhookBot");
        builder.UseContentRoot(path);

        builder.ConfigureAppConfiguration(cfg =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["BOT_TOKEN"] = "000:FAKE",
                ["Transport:Mode"] = "Webhook",
                ["Transport:Webhook:Secret"] = "secret",
                ["Transport:Webhook:PublicUrl"] = "https://example.com",
                ["Storage:Provider"] = "File",
                ["Storage:File:Path"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
            };
            cfg.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            var bot = new Mock<ITelegramBotClient>();
            services.AddSingleton(bot.Object);
            services.AddSingleton<ITransportClient, FakeTransportClient>();
            services.AddSingleton<IStateStore, InMemoryStateStore>();
            services.AddSingleton<IStateStorage>(sp => sp.GetRequiredService<IStateStore>());
        });
    }
}
