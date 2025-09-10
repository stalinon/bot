using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.WebApp.MinimalApi;

using Xunit;

namespace Stalinon.Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Тесты регистрации эндпоинта авторизации Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется выдача JWT при валидных данных.</item>
///         <item>Проверяется отказ при невалидных данных.</item>
///     </list>
/// </remarks>
public sealed class EndpointRouteBuilderExtensionsTests
{
    /// <summary>
    ///     Тест 1: Валидный <c>initData</c> возвращает JWT.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Валидный initData возвращает JWT")]
    public async Task Should_ReturnJwt_When_InitDataValid()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<IWebAppInitDataValidator>(new StubValidator(true));
                services.Configure<WebAppAuthOptions>(o =>
                {
                    o.Secret = "0123456789ABCDEF0123456789ABCDEF";
                    o.Lifetime = TimeSpan.FromMinutes(5);
                });
                services.AddSingleton<WebAppStatsCollector>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapWebAppAuth(); });
            });
        using var server = new TestServer(builder);
        var client = server.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        var initData = "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%7D&auth_date=1&hash=abc";
        var resp = await client.PostAsJsonAsync("/webapp/auth", new { initData });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        json.Should().NotBeNull();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(json!["token"]);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "1");
    }

    /// <summary>
    ///     Тест 2: Невалидный <c>initData</c> возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Невалидный initData возвращает 401")]
    public async Task Should_Return401_When_InitDataInvalid()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<IWebAppInitDataValidator>(new StubValidator(false));
                services.Configure<WebAppAuthOptions>(o =>
                {
                    o.Secret = "0123456789ABCDEF0123456789ABCDEF";
                    o.Lifetime = TimeSpan.FromMinutes(5);
                });
                services.AddSingleton<WebAppStatsCollector>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapWebAppAuth(); });
            });
        using var server = new TestServer(builder);
        var client = server.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        var initData = "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%7D&auth_date=1&hash=abc";
        var resp = await client.PostAsJsonAsync("/webapp/auth", new { initData });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
}

