using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты административного API.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется авторизация для административных эндпоинтов.</item>
///         <item>Проверяется работа health-проб.</item>
///         <item>Проверяется наличие агрегированных метрик.</item>
///         <item>Проверяет эндпоинт статистики.</item>
///         <item>Проверяет эндпоинт рассылки.</item>
///         <item>Проверяет пробы готовности.</item>
///         <item>Проверяет эндпоинт пользовательских метрик.</item>
///     </list>
/// </remarks>
public class AdminApiTests : IClassFixture<AdminApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminApiTests(AdminApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<AdminOptions>(opts => opts.AdminToken = "secret");
                services.AddSingleton<IStateStore, DummyStateStore>();
                services.AddSingleton<ITransportClient, DummyTransportClient>();
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
    ///     Тест 2: Статистика с токеном возвращает 200
    /// </summary>
    [Fact(DisplayName = "Тест 2: Статистика с токеном возвращает 200")]
    public async Task Should_Return200_When_StatsWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 3: Рассылка без токена возвращает 401
    /// </summary>
    [Fact(DisplayName = "Тест 3: Рассылка без токена возвращает 401")]
    public async Task Should_Return401_When_BroadcastWithoutToken()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/admin/broadcast",
            new { chatIds = new List<long> { 1 } });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Тест 4: Рассылка с токеном возвращает 200
    /// </summary>
    [Fact(DisplayName = "Тест 4: Рассылка с токеном возвращает 200")]
    public async Task Should_Return200_When_BroadcastWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/broadcast");
        request.Headers.Add("X-Admin-Token", "secret");
        request.Content = JsonContent.Create(new { chatIds = new List<long> { 1, 2 } });
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 5: Живая проба возвращает 200
    /// </summary>
    [Fact(DisplayName = "Тест 5: Живая проба возвращает 200")]
    public async Task Should_Return200_On_HealthLive()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/live");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 6: Готовность возвращает 200
    /// </summary>
    [Fact(DisplayName = "Тест 6: Готовность возвращает 200")]
    public async Task Should_Return200_On_HealthReady()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 7: При переполнении очереди готовность возвращает 503
    /// </summary>
    [Fact(DisplayName = "Тест 7: При переполнении очереди готовность возвращает 503")]
    public async Task Should_Return503_When_QueueOverflow()
    {
        var stats = _factory.Services.GetRequiredService<StatsCollector>();
        stats.SetQueueDepth(1001);

        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Тест 8: При ошибке пробы готовность возвращает 503.
    /// </summary>
    [Fact(DisplayName = "Тест 8: При ошибке пробы готовность возвращает 503")]
    public async Task Should_Return503_When_HealthReadyProbeFails()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ITransportClient, FailingTransportClient>();
            });
        });
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Тест 9: При ошибке хранилища готовность возвращает 503
    /// </summary>
    [Fact(DisplayName = "Тест 9: При ошибке хранилища готовность возвращает 503")]
    public async Task Should_Return503_When_StorageFails()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => { services.AddSingleton<IStateStore, FailingStateStore>(); });
        });

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Тест 10: При ошибке транспорта готовность возвращает 503
    /// </summary>
    [Fact(DisplayName = "Тест 10: При ошибке транспорта готовность возвращает 503")]
    public async Task Should_Return503_When_TransportFails()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ITransportClient, FailingTransportClient>();
            });
        });

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    ///     Тест 11: Статистика содержит агрегированные метрики, включая <c>web_app_data</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 11: Статистика содержит агрегированные метрики, включая web_app_data")]
    public async Task Should_ContainAggregatedMetrics_When_StatsRequestedWithToken()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().ContainKeys("p50", "p95", "p99", "rps", "errorRate", "webappAuth", "webappMe", "webappSendData",
            "webappSendDataSuccess", "webappSendDataError", "webappLatencyP50", "webappLatencyP95", "webappLatencyP99");
    }

    /// <summary>
    ///     Тест 12: Пользовательские метрики возвращаются на эндпоинте.
    /// </summary>
    [Fact(DisplayName = "Тест 12: Пользовательские метрики возвращаются на эндпоинте")]
    public async Task Should_ReturnCustomMetrics_When_RequestedWithToken()
    {
        var custom = _factory.Services.GetRequiredService<CustomStats>();
        custom.Increment("foo");
        custom.Record("bar", 2);

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats/custom");
        request.Headers.Add("X-Admin-Token", "secret");
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().ContainKey("counters");
        json["counters"].GetProperty("foo").GetInt64().Should().Be(1);
        json.Should().ContainKey("histograms");
        json["histograms"].GetProperty("bar").GetProperty("p95").GetDouble().Should().BeGreaterOrEqualTo(0);
    }

    private sealed class DummyStateStore : IStateStore
    {
        public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
        {
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl,
            CancellationToken ct)
        {
            return Task.FromResult(false);
        }

        public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
        {
            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.FromResult(0L);
        }

        public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class DummyTransportClient : ITransportClient
    {
        public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailingStateStore : IStateStore
    {
        public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
        {
            return Task.FromException<T?>(new Exception("fail"));
        }

        public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl,
            CancellationToken ct)
        {
            return Task.FromException<bool>(new Exception("fail"));
        }

        public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
        {
            return Task.FromException<bool>(new Exception("fail"));
        }

        public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.FromException<long>(new Exception("fail"));
        }

        public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
        {
            return Task.FromException<bool>(new Exception("fail"));
        }
    }

    private sealed class FailingTransportClient : ITransportClient
    {
        public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }

        public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
        {
            return Task.FromException(new Exception("fail"));
        }
    }
}
