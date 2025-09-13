using System;
using System.IO;
using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Logging;
using Stalinon.Bot.Tests.Shared;

using Xunit;

namespace Stalinon.Bot.Logging.Tests;

/// <summary>
///     Интеграционные тесты логирования бота.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Форматирует сообщение с уровнем и текстом.</item>
///         <item>Фильтрует сообщения по уровню из переменной окружения.</item>
///         <item>Применяет <see cref="BotLoggerOptions"/> при форматировании.</item>
///     </list>
/// </remarks>
public sealed class BotLoggingIntegrationTests
{
    /// <inheritdoc />
    public BotLoggingIntegrationTests()
    {
    }

    /// <summary>
    ///     Тест 1: Форматирует сообщение с уровнем и текстом.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Форматирует сообщение с уровнем и текстом")]
    public void Should_FormatMessage_When_LogWritten()
    {
        using var sw = new StringWriter();
        var original = Console.Out;
        Console.SetOut(sw);
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddBotLogging());
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<BotLoggingIntegrationTests>>();
            logger.LogInformation("hello");
            provider.Dispose();
        }
        finally
        {
            Console.SetOut(original);
        }

        var line = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).First();
        line.Should().Be("[Information] hello");
    }

    /// <summary>
    ///     Тест 2: Фильтрует сообщения по уровню из переменной окружения.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Фильтрует сообщения по уровню из переменной окружения")]
    public void Should_FilterMessages_When_LogLevelSpecified()
    {
        var prev = Environment.GetEnvironmentVariable("LOG_LEVEL");
        Environment.SetEnvironmentVariable("LOG_LEVEL", "Warning");
        var collector = new CollectingLoggerProvider();
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(b =>
            {
                b.AddBotLogging();
                b.AddProvider(collector);
            });
            using var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<BotLoggingIntegrationTests>>();
            logger.LogInformation("info");
            logger.LogWarning("warn");
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOG_LEVEL", prev);
        }

        collector.Logs.Should().ContainSingle();
        var log = collector.Logs.Single();
        log.Level.Should().Be(LogLevel.Warning);
        log.Message.Should().Be("warn");
    }

    /// <summary>
    ///     Тест 3: Применяет <see cref="BotLoggerOptions"/> при форматировании.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Применяет BotLoggerOptions при форматировании")]
    public void Should_ApplyOptions_When_Configured()
    {
        using var sw = new StringWriter();
        var original = Console.Out;
        Console.SetOut(sw);
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddBotLogging(o => o.MaxFieldLength = 3));
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<BotLoggingIntegrationTests>>();
            logger.LogInformation("12345");
            provider.Dispose();
        }
        finally
        {
            Console.SetOut(original);
        }

        var line = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).First();
        line.Should().Be("[Information] 123");
    }
}

