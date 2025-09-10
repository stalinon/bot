using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Logging;

using Xunit;

namespace Stalinon.Bot.Logging.Tests;

/// <summary>
///     Тесты расширений логирования.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется регистрация BotConsoleFormatter.</item>
///         <item>Проверяется использование значений по умолчанию в BotLoggerOptions.</item>
///         <item>Проверяется переопределение BotLoggerOptions.</item>
///     </list>
/// </remarks>
public sealed class LoggingBuilderExtensionsTests
{
    /// <inheritdoc />
    public LoggingBuilderExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует BotConsoleFormatter.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует BotConsoleFormatter")]
    public void Should_RegisterBotConsoleFormatter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddBotLogging());

        // Act
        var provider = services.BuildServiceProvider();
        var formatters = provider.GetServices<ConsoleFormatter>();
        var options = provider.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;

        // Assert
        formatters.OfType<BotConsoleFormatter>().Should().ContainSingle();
        options.FormatterName.Should().Be(BotConsoleFormatter.FormatterName);
    }

    /// <summary>
    ///     Тест 2: Использует значения по умолчанию в BotLoggerOptions.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Использует значения по умолчанию в BotLoggerOptions")]
    public void Should_UseDefaultBotLoggerOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddBotLogging());

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BotLoggerOptions>>().Value;

        // Assert
        options.MaxFieldLength.Should().Be(1024);
        options.Sampling.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 3: Переопределяет BotLoggerOptions через делегат.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Переопределяет BotLoggerOptions через делегат")]
    public void Should_OverrideBotLoggerOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddBotLogging(o =>
        {
            o.MaxFieldLength = 10;
            o.Sampling["noise"] = 0.1;
        }));

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BotLoggerOptions>>().Value;

        // Assert
        options.MaxFieldLength.Should().Be(10);
        options.Sampling.Should().ContainKey("noise");
        options.Sampling["noise"].Should().Be(0.1);
    }
}

