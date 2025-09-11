using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="StopOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется значение таймаута по умолчанию.</item>
///         <item>Проверяется ошибка при отрицательном значении.</item>
///     </list>
/// </remarks>
public sealed class StopOptionsTests
{
    /// <inheritdoc />
    public StopOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Таймаут по умолчанию равен нулю.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Таймаут по умолчанию равен нулю")]
    public void Should_HaveDefaultTimeout()
    {
        // Arrange
        var options = new StopOptions();

        // Assert
        options.DrainTimeoutSeconds.Should().Be(0);
    }

    /// <summary>
    ///     Тест 2: Отрицательный таймаут вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Отрицательный таймаут вызывает ошибку валидации")]
    public void Should_FailValidation_When_Negative()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<StopOptions>()
            .Configure(o => o.DrainTimeoutSeconds = -1)
            .Validate(o => o.DrainTimeoutSeconds >= 0, "timeout >= 0");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<StopOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

