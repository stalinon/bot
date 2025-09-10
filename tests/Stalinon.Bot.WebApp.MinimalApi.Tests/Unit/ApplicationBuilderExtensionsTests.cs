using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.WebApp.MinimalApi;

using Xunit;

namespace Stalinon.Bot.WebApp.MinimalApi.Tests;

/// <summary>
///     Тесты регистрации middleware для Mini App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется добавление заголовков CSP.</item>
///     </list>
/// </remarks>
public sealed class ApplicationBuilderExtensionsTests : IClassFixture<WebAppApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <inheritdoc />
    public ApplicationBuilderExtensionsTests(WebAppApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Добавляет CSP-заголовки при регистрации middleware.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Добавляет CSP-заголовки при регистрации middleware")]
    public async Task Should_AddCspHeaders_When_MiddlewareRegistered()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddRouting();
            });
            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseStrictCspForWebApp(["https://example.com"]);
                app.UseEndpoints(endpoints => { endpoints.MapGet("/", () => Results.Ok()); });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var resp = await client.GetAsync("/");
        resp.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        resp.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("https://example.com");
        resp.Headers.Contains("Referrer-Policy").Should().BeTrue();
        resp.Headers.GetValues("Referrer-Policy").Single().Should().Be("no-referrer");
        resp.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        resp.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        resp.Headers.Contains("X-Frame-Options").Should().BeTrue();
        resp.Headers.GetValues("X-Frame-Options").Single().Should().Be("DENY");
    }
}

