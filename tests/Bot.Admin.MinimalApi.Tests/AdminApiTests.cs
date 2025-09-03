using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты административного API.
/// </summary>
public class AdminApiTests : IClassFixture<AdminApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Создаёт экземпляр тестов.
    /// </summary>
    public AdminApiTests(AdminApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<AdminOptions>(opts => opts.AdminToken = "secret");
            });
        });
    }

    /// <summary>
    ///     Проверяет, что статистика без токена возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Статистика без токена возвращает 401")]
    public async Task Stats_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/admin/stats");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что статистика с токеном возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 2. Статистика с токеном возвращает 200")]
    public async Task Stats_with_token_returns_200()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что рассылка без токена возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 3. Рассылка без токена возвращает 401")]
    public async Task Broadcast_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/admin/broadcast", new { chatIds = new long[] { 1 } });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что рассылка с токеном возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 4. Рассылка с токеном возвращает 200")]
    public async Task Broadcast_with_token_returns_200()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/broadcast");
        request.Headers.Add("X-Admin-Token", "secret");
        request.Content = JsonContent.Create(new { chatIds = new long[] { 1, 2 } });
        var resp = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что живая проба возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 5. Живая проба возвращает 200")]
    public async Task Health_live_returns_200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что готовность возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 6. Готовность возвращает 200")]
    public async Task Health_ready_returns_200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что при ошибке пробы готовность возвращает 503.
    /// </summary>
    [Fact(DisplayName = "Тест 7. При ошибке пробы готовность возвращает 503")]
    public async Task Health_ready_returns_503_when_probe_fails()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHealthProbe, FailingProbe>();
            });
        });
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);
    }

    private sealed class FailingProbe : IHealthProbe
    {
        public Task ProbeAsync(CancellationToken ct) => Task.FromException(new Exception("fail"));
    }
}

