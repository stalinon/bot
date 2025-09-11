using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="WebhookOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются значения по умолчанию.</item>
///         <item>Проверяется ошибка при отрицательной ёмкости очереди.</item>
///     </list>
/// </remarks>
public sealed class WebhookOptionsTests
{
    /// <inheritdoc />
    public WebhookOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Значения по умолчанию корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Значения по умолчанию корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new WebhookOptions();

        // Assert
        options.PublicUrl.Should().BeNull();
        options.Secret.Should().BeEmpty();
        options.QueueCapacity.Should().Be(1024);
    }

    /// <summary>
    ///     Тест 2: Отрицательная ёмкость вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Отрицательная ёмкость вызывает ошибку валидации")]
    public void Should_FailValidation_When_QueueCapacityNegative()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<WebhookOptions>()
            .Configure(o => o.QueueCapacity = -1)
            .Validate(o => o.QueueCapacity > 0, "capacity > 0");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<WebhookOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

