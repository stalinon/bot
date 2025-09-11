using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace Stalinon.Bot.Examples.WebAppDemo.Tests.Integration;

/// <summary>
///     Интеграционные тесты WebAppDemo: маршруты и авторизация.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отдаёт статическую страницу по корню.</item>
///         <item>Выдаёт JWT при валидном <c>initData</c>.</item>
///     </list>
/// </remarks>
public sealed class WebAppDemoIntegrationTests : IClassFixture<WebAppDemoFactory>
{
    private readonly WebAppDemoFactory _factory;

    /// <inheritdoc />
    public WebAppDemoIntegrationTests(WebAppDemoFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Должен отдавать статическую страницу по корневому маршруту.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен отдавать статическую страницу по корневому маршруту.")]
    public async Task Should_ReturnIndexHtml_OnRoot()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var resp = await client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("<title>Mini App Demo</title>");
    }

    /// <summary>
    ///     Тест 2: Должен выдавать JWT при валидном <c>initData</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выдавать JWT при валидном initData.")]
    public async Task Should_ReturnJwt_OnValidInitData()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var initData =
            "user=%7B%22id%22%3A1%2C%22username%22%3A%22test%22%2C%22language_code%22%3A%22ru%22%7D&auth_date=1&hash=abc";
        var resp = await client.PostAsJsonAsync("/webapp/auth", new { initData });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        json.Should().NotBeNull();
        json!["token"].GetString().Should().NotBeNullOrEmpty();
    }
}
