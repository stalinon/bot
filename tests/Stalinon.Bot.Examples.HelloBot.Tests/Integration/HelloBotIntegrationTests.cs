using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.Examples.HelloBot.Handlers;
using Stalinon.Bot.Examples.HelloBot.Scenes;
using Stalinon.Bot.Examples.HelloBot.Services;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Examples.HelloBot.Tests.Integration;

/// <summary>
///     Интеграционные тесты HelloBot: обработка команд и конфигурации.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отвечает на <c>/start</c> и <c>/ping</c>.</item>
///         <item>Проходит сценарий <c>/phone</c>.</item>
///         <item>Без <c>BOT_TOKEN</c> падает при конфигурации.</item>
///     </list>
/// </remarks>
public sealed class HelloBotIntegrationTests
{
    private static IHost BuildHost(bool withToken = true)
    {
        var builder = Host.CreateApplicationBuilder();
        var cfg = new Dictionary<string, string?>
        {
            ["Transport:Mode"] = "Polling",
            ["Transport:Parallelism"] = "1",
            ["PHONE_STEP_TTL_SECONDS"] = "60"
        };
        if (withToken)
        {
            cfg["BOT_TOKEN"] = "000:FAKE";
        }

        builder.Configuration.AddInMemoryCollection(cfg);

        builder.Services
            .AddBot(o =>
            {
                o.Token = builder.Configuration["BOT_TOKEN"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
                builder.Configuration.GetSection("Transport").Bind(o.Transport);
                o.RateLimits = new RateLimitOptions
                {
                    PerUserPerMinute = 20,
                    PerChatPerMinute = 60,
                    Mode = RateLimitMode.Soft
                };
            })
            .AddHandlersFromAssembly(typeof(StartHandler).Assembly)
            .UseStateStorage(new InMemoryStateStore())
            .UsePipeline();

        builder.Services.AddSingleton<ITransportClient, FakeTransportClient>();
        builder.Services.AddScoped<RequestIdProvider>();
        builder.Services.AddScoped<ISceneNavigator>(sp =>
            new SceneNavigator(
                sp.GetRequiredService<IStateStore>(),
                TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("PHONE_STEP_TTL_SECONDS"))));
        builder.Services.AddScoped<PhoneScene>(sp =>
            new PhoneScene(
                sp.GetRequiredService<ITransportClient>(),
                sp.GetRequiredService<ISceneNavigator>(),
                TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("PHONE_STEP_TTL_SECONDS"))));
        builder.Services.AddScoped<ProfileScene>(sp =>
            new ProfileScene(
                sp.GetRequiredService<ISceneNavigator>(),
                sp.GetRequiredService<ITransportClient>(),
                TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("PHONE_STEP_TTL_SECONDS"))));

        return builder.Build();
    }

    private static UpdateDelegate BuildApp(IHost host)
    {
        var pipeline = host.Services.GetRequiredService<IUpdatePipeline>();
        foreach (var cfg in host.Services.GetRequiredService<IEnumerable<Action<IUpdatePipeline>>>())
        {
            cfg(pipeline);
        }

        pipeline
            .Use<ExceptionHandlingMiddleware>()
            .Use<MetricsMiddleware>()
            .Use<LoggingMiddleware>()
            .Use<DedupMiddleware>()
            .Use<RateLimitMiddleware>()
            .Use<CommandParsingMiddleware>()
            .Use<RouterMiddleware>();

        return pipeline.Build(_ => ValueTask.CompletedTask);
    }

    /// <summary>
    ///     Тест 1: Должен отвечать на /start и /ping.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен отвечать на /start и /ping")]
    public async Task Should_Respond_OnStartAndPing()
    {
        using var host = BuildHost();
        var app = BuildApp(host);
        var tx = (FakeTransportClient)host.Services.GetRequiredService<ITransportClient>();
        var chat = new ChatAddress(1);
        var user = new UserAddress(1);

        await app(new UpdateContext("tg", "1", chat, user, "/start", null, null, null, new Dictionary<string, object>(), host.Services, default));
        await app(new UpdateContext("tg", "2", chat, user, "/ping", null, null, null, new Dictionary<string, object>(), host.Services, default));

        tx.SentTexts[0].text.Should().Be("привет. я живой. напиши /ping");
        tx.SentTexts[1].text.Should().StartWith("pong #1");
    }

    /// <summary>
    ///     Тест 2: Должен проходить сценарий /phone.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен проходить сценарий /phone")]
    public async Task Should_RunPhoneScene()
    {
        using var host = BuildHost();
        var app = BuildApp(host);
        var tx = (FakeTransportClient)host.Services.GetRequiredService<ITransportClient>();
        var chat = new ChatAddress(2);
        var user = new UserAddress(2);

        UpdateContext C(string text) => new("tg", Guid.NewGuid().ToString(), chat, user, text, null, null, null, new Dictionary<string, object>(), host.Services, default);

        await app(C("/phone"));
        await app(C("+79991234567"));
        await app(C("да"));

        tx.SentTexts.Select(t => t.text).Should().ContainInOrder(
            "введите номер телефона",
            "подтвердите номер: +79991234567 (да/нет)",
            "номер сохранён: +79991234567");
    }

    /// <summary>
    ///     Тест 3: Должен выбрасывать ошибку при отсутствии BOT_TOKEN.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен выбрасывать ошибку при отсутствии BOT_TOKEN")]
    public void Should_Throw_OnMissingToken()
    {
        var act = () =>
        {
            using var host = BuildHost(false);
            host.Services.GetRequiredService<BotHostedService>();
        };

        act.Should().Throw<InvalidOperationException>();
    }
}
