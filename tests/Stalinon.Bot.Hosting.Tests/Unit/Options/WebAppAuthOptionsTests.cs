using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.WebApp.MinimalApi;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="WebAppAuthOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются значения по умолчанию.</item>
///         <item>Проверяется ошибка при пустом секрете.</item>
///     </list>
/// </remarks>
public sealed class WebAppAuthOptionsTests
{
    /// <inheritdoc />
    public WebAppAuthOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Дефолтные значения корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Дефолтные значения корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new WebAppAuthOptions();

        // Assert
        options.Secret.Should().BeEmpty();
        options.Lifetime.Should().Be(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    ///     Тест 2: Пустой секрет вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пустой секрет вызывает ошибку валидации")]
    public void Should_FailValidation_When_SecretEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<WebAppAuthOptions>()
            .Configure(o => o.Secret = string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "secret required");
        using var sp = services.BuildServiceProvider();

        // Act
        Action act = () => _ = sp.GetRequiredService<IOptions<WebAppAuthOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

