using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты авторизации административных эндпоинтов.
/// </summary>
public class AdminAuthTests
{
    /// <summary>
    ///     Проверяет, что без токена возвращается 401.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Без токена возвращается 401")]
    public async Task Without_token_returns_401()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<StatsCollector>();
                services.AddSingleton<WebAppStatsCollector>();
                services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AdminOptions
                {
                    AdminToken = "secret"
                }));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapAdminStats());
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();

        var resp = await client.GetAsync("/admin/stats");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>
    ///     Проверяет, что с правильным токеном возвращается 200.
    /// </summary>
    [Fact(DisplayName = "Тест 2. С правильным токеном возвращается 200")]
    public async Task With_token_returns_200()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<StatsCollector>();
                services.AddSingleton<WebAppStatsCollector>();
                services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AdminOptions
                {
                    AdminToken = "secret"
                }));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapAdminStats());
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/stats");
        request.Headers.Add("X-Admin-Token", "secret");

        var resp = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
