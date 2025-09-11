using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="BotOptions" />: проверка значений по умолчанию и валидации.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются дефолтные значения.</item>
///         <item>Проверяется ошибка при пустом токене.</item>
///     </list>
/// </remarks>
public sealed class BotOptionsTests
{
    /// <inheritdoc />
    public BotOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Значения по умолчанию корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Значения по умолчанию корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new BotOptions();

        // Assert
        options.Token.Should().BeEmpty();
        options.AdminToken.Should().BeEmpty();
        options.Transport.Mode.Should().Be(TransportMode.Polling);
        options.Transport.Parallelism.Should().Be(8);
        options.DrainTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    ///     Тест 2: Пустой токен вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пустой токен вызывает ошибку валидации")]
    public void Should_FailValidation_When_TokenEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<BotOptions>()
            .Configure(o => o.Token = string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Token), "token required");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<BotOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

