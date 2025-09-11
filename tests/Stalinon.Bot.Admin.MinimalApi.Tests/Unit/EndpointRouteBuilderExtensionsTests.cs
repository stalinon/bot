using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Outbox;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты регистрации административных эндпоинтов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет подключение всех маршрутов API.</item>
///     </list>
/// </remarks>
public sealed class EndpointRouteBuilderExtensionsTests
{
    /// <inheritdoc />
    public EndpointRouteBuilderExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен подключать все маршруты административного API.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен подключать все маршруты административного API.")]
    public void Should_MapAllAdminEndpoints()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAdminApi(new ConfigurationBuilder().Build());
        builder.Services.AddSingleton<IOutbox, DummyOutbox>();
        var app = builder.Build();

        // Act
        app.MapAdminApi();

        // Assert
        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
        routes.Should().Contain(e => e.RoutePattern.RawText == "/health/live");
        routes.Should().Contain(e => e.RoutePattern.RawText == "/health/ready");
        routes.Should().Contain(e => e.RoutePattern.RawText == "/admin/stats");
        routes.Should().Contain(e => e.RoutePattern.RawText == "/admin/stats/custom");
        routes.Should().Contain(e => e.RoutePattern.RawText == "/admin/broadcast");
        routes.Should().Contain(e => e.RoutePattern.RawText == "/admin/outbox/pending");
    }

    private sealed class DummyOutbox : IOutbox
    {
        public Task SendAsync(string id, string payload, Func<string, string, CancellationToken, Task> transport, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<long> GetPendingAsync(CancellationToken ct)
        {
            return Task.FromResult(0L);
        }
    }
}
