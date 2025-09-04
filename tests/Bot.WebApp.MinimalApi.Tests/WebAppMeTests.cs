using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Bot.Abstractions.Contracts;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Xunit;

namespace Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Тесты эндпоинта профиля Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет отказ без токена.</item>
///         <item>Проверяет успешное получение профиля при валидном токене.</item>
///         <item>Проверяет отказ при использовании незащищённого протокола.</item>
///     </list>
/// </remarks>
public sealed class WebAppMeTests : IClassFixture<WebAppApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <inheritdoc />
    public WebAppMeTests(WebAppApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Без токена возвращает 401.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Без токена возвращает 401")]
    public async Task Should_Return401_When_NoToken()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWebAppInitDataValidator, StubValidator>();
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

        var resp = await client.GetAsync("/webapp/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Тест 2: Валидный токен возвращает профиль.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Валидный токен возвращает профиль")]
    public async Task Should_ReturnProfile_When_TokenValid()
    {
        const string secret = "0123456789ABCDEF0123456789ABCDEF";
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWebAppInitDataValidator, StubValidator>();
                services.Configure<WebAppAuthOptions>(o =>
                {
                    o.Secret = secret;
                    o.Lifetime = TimeSpan.FromMinutes(5);
                });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = handler.WriteToken(new JwtSecurityToken(
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim("username", "test"),
                new Claim("language_code", "ru"),
                new Claim("auth_date", "1")
            },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds));

        var request = new HttpRequestMessage(HttpMethod.Get, "/webapp/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().NotBeNull();
        json!["id"].GetInt64().Should().Be(1);
        json["username"].GetString().Should().Be("test");
        json["language_code"].GetString().Should().Be("ru");
        json["auth_date"].GetInt64().Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Незащищённый протокол возвращает 400.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Незащищённый протокол возвращает 400")]
    public async Task Should_Return400_When_NotHttps()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWebAppInitDataValidator, StubValidator>();
                services.Configure<WebAppAuthOptions>(o =>
                {
                    o.Secret = "0123456789ABCDEF0123456789ABCDEF";
                    o.Lifetime = TimeSpan.FromMinutes(5);
                });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var resp = await client.GetAsync("/webapp/me");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class StubValidator : IWebAppInitDataValidator
    {
        public bool TryValidate(string initData, out string? error)
        {
            error = null;
            return true;
        }
    }
}
