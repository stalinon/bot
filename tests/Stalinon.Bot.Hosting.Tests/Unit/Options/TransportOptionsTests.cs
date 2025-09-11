using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="TransportOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются значения по умолчанию.</item>
///         <item>Проверяется ошибка при некорректном параллелизме.</item>
///     </list>
/// </remarks>
public sealed class TransportOptionsTests
{
    /// <inheritdoc />
    public TransportOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Дефолтные значения корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Дефолтные значения корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new TransportOptions();

        // Assert
        options.Mode.Should().Be(TransportMode.Polling);
        options.Parallelism.Should().Be(8);
        options.Webhook.Should().NotBeNull();
    }

    /// <summary>
    ///     Тест 2: Нулевой параллелизм вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Нулевой параллелизм вызывает ошибку валидации")]
    public void Should_FailValidation_When_ParallelismZero()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<TransportOptions>()
            .Configure(o => o.Parallelism = 0)
            .Validate(o => o.Parallelism > 0, "parallelism > 0");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<TransportOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

