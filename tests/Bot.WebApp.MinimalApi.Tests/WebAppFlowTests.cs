using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Routing;
using Bot.Core.Stats;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Тесты полного цикла Mini App: авторизация, профиль и отправка данных.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется получение JWT через <c>/webapp/auth</c>.</item>
///         <item>Проверяется запрос профиля через <c>/webapp/me</c>.</item>
///         <item>Проверяется обработка <c>web_app_data</c>.</item>
///     </list>
/// </remarks>
public sealed class WebAppFlowTests : IClassFixture<WebAppApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <inheritdoc />
    public WebAppFlowTests(WebAppApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Выполняет полный цикл авторизации и отправки данных.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Выполняет полный цикл авторизации и отправки данных.")]
    public async Task Should_CompleteFullFlow()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWebAppInitDataValidator>(new StubValidator(true));
                services.Configure<WebAppAuthOptions>(o =>
                {
                    o.Secret = "0123456789ABCDEF0123456789ABCDEF";
                    o.Lifetime = TimeSpan.FromMinutes(5);
                });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var initData = "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%7D&auth_date=1&hash=abc";
        var authResp = await client.PostAsJsonAsync("/webapp/auth", new { initData });
        authResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await authResp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        json.Should().NotBeNull();
        var token = json!["token"];

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/webapp/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResp = await client.SendAsync(meRequest);
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = new Dictionary<string, object>
        {
            [UpdateItems.UpdateType] = "message",
            [UpdateItems.MessageId] = 2,
            [UpdateItems.WebAppData] = true
        };
        var ctx = new UpdateContext(
            "telegram",
            "1",
            new ChatAddress(1),
            new UserAddress(3),
            "btn",
            null,
            null,
            "42",
            items,
            null!,
            default);
        var handled = false;
        var services = new ServiceCollection();
        services.AddSingleton(new StatsCollector());
        services.AddTransient<TestHandler>(_ => new TestHandler(() => handled = true));
        var sp = services.BuildServiceProvider();
        var registry = new HandlerRegistry();
        registry.Register(typeof(TestHandler));
        var router = new RouterMiddleware(sp, registry, sp.GetRequiredService<StatsCollector>());
        ctx = ctx with { Services = sp };

        await router.InvokeAsync(ctx, _ => Task.CompletedTask);

        handled.Should().BeTrue();
        ctx.Payload.Should().Be("42");
        ctx.GetItem<bool>(UpdateItems.WebAppData).Should().BeTrue();
    }

    private sealed class StubValidator : IWebAppInitDataValidator
    {
        private readonly bool _result;

        public StubValidator(bool result)
        {
            _result = result;
        }

        public bool TryValidate(string initData, out string? error)
        {
            error = _result ? null : "err";
            return _result;
        }
    }

    [TextMatch(".*")]
    [UpdateFilter(WebAppData = true)]
    private sealed class TestHandler(Action onHandled) : IUpdateHandler
    {
        private readonly Action _onHandled = onHandled;

        public Task HandleAsync(UpdateContext ctx)
        {
            _onHandled();
            return Task.CompletedTask;
        }
    }
}
