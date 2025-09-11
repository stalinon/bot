using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="OutboxOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется путь по умолчанию.</item>
///         <item>Проверяется ошибка при пустом пути.</item>
///     </list>
/// </remarks>
public sealed class OutboxOptionsTests
{
    /// <inheritdoc />
    public OutboxOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Путь по умолчанию корректен.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Путь по умолчанию корректен")]
    public void Should_HaveDefaultPath()
    {
        // Arrange
        var options = new OutboxOptions();

        // Assert
        options.Path.Should().Be("outbox");
    }

    /// <summary>
    ///     Тест 2: Пустой путь вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пустой путь вызывает ошибку валидации")]
    public void Should_FailValidation_When_PathEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<OutboxOptions>()
            .Configure(o => o.Path = string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Path), "path required");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

