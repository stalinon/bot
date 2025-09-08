using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions.Contracts;

using Xunit;

namespace Stalinon.Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Тесты эндпоинта авторизации Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется успешная выдача JWT при валидных данных.</item>
///         <item>Проверяется отказ при невалидных данных.</item>
///     </list>
/// </remarks>
public sealed class WebAppAuthTests : IClassFixture<WebAppApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <inheritdoc />
    public WebAppAuthTests(WebAppApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Валидный <c>initData</c> возвращает JWT.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Валидный initData возвращает JWT")]
    public async Task Should_ReturnJwt_When_InitDataValid()
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

        var initData =
            "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%2C%22language_code%22%3A%22ru%22%7D&auth_date=1&hash=abc";
        var resp = await client.PostAsJsonAsync("/webapp/auth", new { initData });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().NotBeNull();
        var token = json!["token"].GetString();
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "1");
        jwt.Claims.Should().Contain(c => c.Type == "language_code" && c.Value == "ru");
        jwt.Claims.Should().Contain(c => c.Type == "auth_date" && c.Value == "1");
    }

    /// <summary>
    ///     Тест 2: Невалидный <c>initData</c> возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Невалидный initData возвращает 401")]
    public async Task Should_Return401_When_InitDataInvalid()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWebAppInitDataValidator>(new StubValidator(false));
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

        var initData =
            "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%2C%22language_code%22%3A%22ru%22%7D&auth_date=1&hash=abc";
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
