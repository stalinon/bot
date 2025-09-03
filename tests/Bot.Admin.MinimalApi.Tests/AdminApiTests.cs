using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты административного API.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется авторизация для административных эндпоинтов.</item>
///         <item>Проверяется работа health-проб.</item>
///         <item>Проверяется наличие агрегированных метрик.</item>
///     </list>
/// </remarks>
public class AdminApiTests : IClassFixture<AdminApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <inheritdoc />
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
    ///     Тест 1: Статистика без токена возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Статистика без токена возвращает 401")]
    public async Task Should_Return401_When_StatsRequestedWithoutToken()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/admin/stats");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Тест 2: Статистика с токеном возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Статистика с токеном возвращает 200")]
    public async Task Should_Return200_When_StatsRequestedWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 3: Рассылка без токена возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Рассылка без токена возвращает 401")]
    public async Task Should_Return401_When_BroadcastWithoutToken()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/admin/broadcast", new { chatIds = new long[] { 1 } });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Тест 4: Рассылка с токеном возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Рассылка с токеном возвращает 200")]
    public async Task Should_Return200_When_BroadcastWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/broadcast");
        request.Headers.Add("X-Admin-Token", "secret");
        request.Content = JsonContent.Create(new { chatIds = new long[] { 1, 2 } });
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 5: Живая проба возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Живая проба возвращает 200")]
    public async Task Should_Return200_When_HealthLiveRequested()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/live");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 6: Готовность возвращает 200.
    /// </summary>
    [Fact(DisplayName = "Тест 6: Готовность возвращает 200")]
    public async Task Should_Return200_When_HealthReadyRequested()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 7: При ошибке пробы готовность возвращает 503.
    /// </summary>
    [Fact(DisplayName = "Тест 7: При ошибке пробы готовность возвращает 503")]
    public async Task Should_Return503_When_HealthReadyProbeFails()
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
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Тест 8: Статистика содержит агрегированные метрики.
    /// </summary>
    [Fact(DisplayName = "Тест 8: Статистика содержит агрегированные метрики")]
    public async Task Should_ContainAggregatedMetrics_When_StatsRequestedWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().ContainKeys("p50", "p95", "p99", "rps", "errorRate");
    }

    private sealed class FailingProbe : IHealthProbe
    {
        public Task ProbeAsync(CancellationToken ct) => Task.FromException(new Exception("fail"));
    }
}

