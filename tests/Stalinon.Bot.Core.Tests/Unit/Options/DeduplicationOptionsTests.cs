using System;
using System.ComponentModel.DataAnnotations;

using FluentAssertions;

using Stalinon.Bot.Core.Options;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты опций дедупликации.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются значения по умолчанию.</item>
///         <item>Проверяется валидация окна.</item>
///     </list>
/// </remarks>
public sealed class DeduplicationOptionsTests
{
    /// <inheritdoc />
    public DeduplicationOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен иметь значения по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен иметь значения по умолчанию.")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var sut = new DeduplicationOptions();

        // Act
        // Assert
        sut.Window.Should().Be(TimeSpan.FromMinutes(5));
        sut.Mode.Should().Be(RateLimitMode.Hard);
        FluentActions.Invoking(() => Validator.ValidateObject(sut, new ValidationContext(sut), true)).Should().NotThrow();
    }

    /// <summary>
    ///     Тест 2: Должен валидировать положительное окно.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен валидировать положительное окно.")]
    public void Should_Throw_WhenWindowNonPositive()
    {
        // Arrange
        var sut = new DeduplicationOptions { Window = TimeSpan.Zero };

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }
}
