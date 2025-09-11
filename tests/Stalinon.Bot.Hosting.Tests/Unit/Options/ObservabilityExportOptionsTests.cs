using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="ObservabilityExportOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется значение OTLP по умолчанию.</item>
///         <item>Проверяется ошибка при запрете OTLP.</item>
///     </list>
/// </remarks>
public sealed class ObservabilityExportOptionsTests
{
    /// <inheritdoc />
    public ObservabilityExportOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: OTLP выключен по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: OTLP выключен по умолчанию")]
    public void Should_HaveDefaultOtlp()
    {
        // Arrange
        var options = new ObservabilityExportOptions();

        // Assert
        options.Otlp.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 2: Запрет OTLP вызывает ошибку.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Запрет OTLP вызывает ошибку")]
    public void Should_FailValidation_When_OtlpDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<ObservabilityExportOptions>()
            .Configure(o => o.Otlp = false)
            .Validate(o => o.Otlp, "otlp must be enabled");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<ObservabilityExportOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

