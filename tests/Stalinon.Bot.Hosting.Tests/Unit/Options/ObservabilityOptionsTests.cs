using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="ObservabilityOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется наличие экспортёров по умолчанию.</item>
///         <item>Проверяется ошибка при отсутствии экспортёров.</item>
///     </list>
/// </remarks>
public sealed class ObservabilityOptionsTests
{
    /// <inheritdoc />
    public ObservabilityOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Имеет экспорт по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Имеет экспорт по умолчанию")]
    public void Should_HaveDefaultExport()
    {
        // Arrange
        var options = new ObservabilityOptions();

        // Assert
        options.Export.Should().NotBeNull();
        options.Export.Otlp.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 2: Отсутствие экспортёров вызывает ошибку.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Отсутствие экспортёров вызывает ошибку")]
    public void Should_FailValidation_When_ExportNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<ObservabilityOptions>()
            .Configure(o => o.Export = null!)
            .Validate(o => o.Export is not null, "export required");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

