using System;
using System.ComponentModel.DataAnnotations;

using FluentAssertions;

using Stalinon.Bot.Core.Options;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты опций ограничения запросов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются значения по умолчанию.</item>
///         <item>Проверяется валидация числовых параметров и окна.</item>
///     </list>
/// </remarks>
public sealed class RateLimitOptionsTests
{
    /// <inheritdoc />
    public RateLimitOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен иметь значения по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен иметь значения по умолчанию.")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var sut = new RateLimitOptions();

        // Act
        // Assert
        sut.PerUserPerMinute.Should().Be(20);
        sut.PerChatPerMinute.Should().Be(60);
        sut.Mode.Should().Be(RateLimitMode.Hard);
        sut.UseStateStore.Should().BeFalse();
        sut.Window.Should().Be(TimeSpan.FromMinutes(1));
        FluentActions.Invoking(() => Validator.ValidateObject(sut, new ValidationContext(sut), true)).Should().NotThrow();
    }

    /// <summary>
    ///     Тест 2: Должен валидировать положительный лимит пользователя.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен валидировать положительный лимит пользователя.")]
    public void Should_Throw_WhenPerUserNonPositive()
    {
        // Arrange
        var sut = new RateLimitOptions { PerUserPerMinute = 0 };

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    ///     Тест 3: Должен валидировать положительный лимит чата.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен валидировать положительный лимит чата.")]
    public void Should_Throw_WhenPerChatNonPositive()
    {
        // Arrange
        var sut = new RateLimitOptions { PerChatPerMinute = 0 };

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    ///     Тест 4: Должен валидировать положительное окно.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен валидировать положительное окно.")]
    public void Should_Throw_WhenWindowNonPositive()
    {
        // Arrange
        var sut = new RateLimitOptions { Window = TimeSpan.Zero };

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }
}
