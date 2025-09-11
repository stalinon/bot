using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests.Options;

/// <summary>
///     Тесты <see cref="WebAppOptions" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются дефолтные значения.</item>
///         <item>Проверяется ошибка при пустом URL.</item>
///     </list>
/// </remarks>
public sealed class WebAppOptionsTests
{
    /// <inheritdoc />
    public WebAppOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Дефолтные значения корректны.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Дефолтные значения корректны")]
    public void Should_HaveDefaults()
    {
        // Arrange
        var options = new WebAppOptions();

        // Assert
        options.PublicUrl.Should().BeEmpty();
        options.AuthTtlSeconds.Should().Be(300);
        options.InitDataTtlSeconds.Should().Be(300);
        options.Csp.Should().NotBeNull();
    }

    /// <summary>
    ///     Тест 2: Пустой URL вызывает ошибку валидации.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пустой URL вызывает ошибку валидации")]
    public void Should_FailValidation_When_PublicUrlEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<WebAppOptions>()
            .Configure(o => o.PublicUrl = string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.PublicUrl), "url required");
        using var sp = services.BuildServiceProvider();

        Action act = () => _ = sp.GetRequiredService<IOptions<WebAppOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}

