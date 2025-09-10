using System;
using System.ComponentModel.DataAnnotations;

using FluentAssertions;

using Stalinon.Bot.Core.Options;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты настроек очереди.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется значение по умолчанию.</item>
///         <item>Проверяется валидация политики.</item>
///     </list>
/// </remarks>
public sealed class QueueOptionsTests
{
    /// <inheritdoc />
    public QueueOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен иметь политику ожидания по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен иметь политику ожидания по умолчанию.")]
    public void Should_HaveDefaultPolicy()
    {
        // Arrange
        var sut = new QueueOptions();

        // Act
        // Assert
        sut.Policy.Should().Be(QueuePolicy.Wait);
        FluentActions.Invoking(() => Validator.ValidateObject(sut, new ValidationContext(sut), true)).Should().NotThrow();
    }

    /// <summary>
    ///     Тест 2: Должен валидировать наличие политики.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен валидировать наличие политики.")]
    public void Should_Throw_WhenPolicyUnknown()
    {
        // Arrange
        var sut = new QueueOptions { Policy = (QueuePolicy)100 };

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }
}
