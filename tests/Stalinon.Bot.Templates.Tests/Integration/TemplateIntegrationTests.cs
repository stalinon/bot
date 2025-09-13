using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.TestKit;
using Stalinon.Bot.WebApp.MinimalApi;

using Telegram.Bot;

using Xunit;

namespace Stalinon.Bot.Templates.Tests.Integration;

/// <summary>
///     Интеграционные тесты шаблона: проверка конфигурации и опций.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запуск в режимах polling и webhook.</item>
///         <item>Обработка отсутствия обязательных переменных.</item>
///         <item>Регистрация Admin и WebApp при включении опций.</item>
///     </list>
/// </remarks>
public sealed class TemplateIntegrationTests
{
    private static WebApplication BuildApp(
        bool withToken = true,
        string mode = "Polling",
        bool withWebhookUrl = true,
        bool admin = false,
        bool webapp = false)
    {
        var builder = WebApplication.CreateBuilder();
        var cfg = new Dictionary<string, string?>
        {
            ["Bot:Transport:Mode"] = mode,
            ["Bot:Transport:Parallelism"] = "1"
        };
        if (mode == "Webhook" && withWebhookUrl)
        {
            cfg["Bot:Transport:Webhook:PublicUrl"] = "https://example.com";
        }
        if (withToken)
        {
            cfg["BOT_TOKEN"] = "000:FAKE";
        }

        builder.Configuration.AddInMemoryCollection(cfg);

        builder.Services
            .AddBot(o =>
            {
                o.Token = builder.Configuration["BOT_TOKEN"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
                builder.Configuration.GetSection("Bot:Transport").Bind(o.Transport);
            })
            .UsePipeline()
            .UseStateStorage(new InMemoryStateStore());
        builder.Services.AddSingleton<ITransportClient, FakeTransportClient>();
        builder.Services.AddSingleton<IUpdateSource, DummyUpdateSource>();
        if (webapp)
        {
            builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient("1:token", new HttpClient()));
            builder.Services.AddMemoryCache();
            builder.Services.AddWebApp(builder.Configuration);
        }
        if (admin)
        {
            builder.Services.AddAdminApi(builder.Configuration);
        }

        var app = builder.Build();
        if (webapp)
        {
            app.MapWebAppAuth();
            app.MapWebAppMe();
        }
        if (admin)
        {
            app.MapHealth();
            app.MapAdminStats();
        }

        return app;
    }

    /// <summary>
    ///     Тест 1: Должен запускаться при режиме polling.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен запускаться при режиме polling.")]
    public void Should_Start_OnPolling()
    {
        using var app = BuildApp();
        var act = () => app.Services.GetRequiredService<BotHostedService>();

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Тест 2: Должен запускаться при режиме webhook.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен запускаться при режиме webhook.")]
    public void Should_Start_OnWebhook()
    {
        using var app = BuildApp(mode: "Webhook");
        var act = () => app.Services.GetRequiredService<BotHostedService>();

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Тест 3: Должен выбрасывать ошибку при отсутствии BOT_TOKEN.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен выбрасывать ошибку при отсутствии BOT_TOKEN.")]
    public void Should_Throw_OnMissingToken()
    {
        var act = () =>
        {
            using var app = BuildApp(withToken: false);
            app.Services.GetRequiredService<BotHostedService>();
        };

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    ///     Тест 4: Должен регистрировать Admin и WebApp при включении опций.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен регистрировать Admin и WebApp при включении опций.")]
    public void Should_Register_AdminAndWebApp()
    {
        using var app = BuildApp(admin: true, webapp: true);
        var probes = app.Services.GetServices<IHealthProbe>();
        var responder = app.Services.GetService<IWebAppQueryResponder>();

        probes.Should().NotBeEmpty();
        responder.Should().NotBeNull();
    }

    private sealed class DummyUpdateSource : IUpdateSource
    {
        public Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;
    }
}
