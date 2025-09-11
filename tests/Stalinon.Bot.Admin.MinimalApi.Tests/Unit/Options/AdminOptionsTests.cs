using System.ComponentModel.DataAnnotations;

using FluentAssertions;

using Stalinon.Bot.Admin.MinimalApi;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты настроек административного API.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется значение токена по умолчанию.</item>
///         <item>Проверяется валидация отсутствующего токена.</item>
///     </list>
/// </remarks>
public sealed class AdminOptionsTests
{
    /// <inheritdoc />
    public AdminOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен иметь пустой токен по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен иметь пустой токен по умолчанию.")]
    public void Should_HaveEmptyToken_ByDefault()
    {
        // Arrange
        var sut = new AdminOptions();

        // Act
        // Assert
        sut.AdminToken.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 2: Должен валидировать отсутствие токена.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен валидировать отсутствие токена.")]
    public void Should_Throw_WhenTokenMissing()
    {
        // Arrange
        var sut = new AdminOptions();

        // Act
        var act = () => Validator.ValidateObject(sut, new ValidationContext(sut), true);

        // Assert
        act.Should().Throw<ValidationException>();
    }
}
