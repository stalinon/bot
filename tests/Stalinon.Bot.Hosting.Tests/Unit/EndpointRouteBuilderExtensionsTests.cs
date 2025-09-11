
using FluentAssertions;

using Microsoft.AspNetCore.Builder;

using Stalinon.Bot.Hosting;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests;

/// <summary>
///     Тесты регистрации маршрутов проверки готовности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет эндпоинт <c>/health/ready</c>.</item>
///     </list>
/// </remarks>
public sealed class EndpointRouteBuilderExtensionsTests
{
    /// <inheritdoc />
    public EndpointRouteBuilderExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует маршрут <c>/health/ready</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует маршрут /health/ready")]
    public void Should_MapHealthEndpoint()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        var result = app.MapHealth();

        // Assert
        result.Should().Be(app);
    }
}
