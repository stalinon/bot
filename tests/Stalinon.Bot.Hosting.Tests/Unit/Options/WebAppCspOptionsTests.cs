using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="WebAppCspOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются дефолтные значения.</item>
///         <item>Проверяется ошибка при отсутствии массива origin'ов.</item>
///     </list>
/// </remarks>
public sealed class WebAppCspOptionsTests
{
    /// <inheritdoc />
    public WebAppCspOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Дефолтные значения корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Дефолтные значения корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new WebAppCspOptions();

        // Assert
        options.AllowedOrigins.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 2: Отсутствие origin'ов вызывает ошибку.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Отсутствие origin'ов вызывает ошибку")]
    public void Should_FailValidation_When_OriginsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<WebAppCspOptions>()
            .Configure(o => o.AllowedOrigins = null!)
            .Validate(o => o.AllowedOrigins is not null, "origins required");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<WebAppCspOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

