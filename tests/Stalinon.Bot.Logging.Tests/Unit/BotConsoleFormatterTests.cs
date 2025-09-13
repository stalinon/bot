using System;
using System.Collections.Generic;
using System.IO;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Logging;

using Xunit;

namespace Stalinon.Bot.Logging.Tests;

/// <summary>
///     Тесты форматтера логов бота.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется усечение длинных полей.</item>
///         <item>Проверяется маскирование секретов и токенов.</item>
///         <item>Проверяется сэмплирование сообщений.</item>
///     </list>
/// </remarks>
public sealed class BotConsoleFormatterTests
{
    /// <summary>
    ///     Тест 1: Длинное поле усечено до заданной длины
    /// </summary>
    [Fact(DisplayName = "Тест 1: Длинное поле усечено до заданной длины")]
    public void Should_Truncate_LongField()
    {
        var opts = new BotLoggerOptions { MaxFieldLength = 5 };
        var formatter = new BotConsoleFormatter(new TestMonitor(opts));
        var state = new List<KeyValuePair<string, object?>> { new("Text", "1234567890") };
        var entry = new LogEntry<List<KeyValuePair<string, object?>>>(LogLevel.Information,
            "cat",
            new EventId(0),
            state,
            null,
            (s, e) => "msg");
        using var sw = new StringWriter();
        formatter.Write(entry, null, sw);
        sw.ToString().Should().Contain("Text=12345");
    }

    /// <summary>
    ///     Тест 2: Секреты заменяются на маску
    /// </summary>
    [Fact(DisplayName = "Тест 2: Секреты заменяются на маску")]
    public void Should_Mask_Secrets()
    {
        var formatter = new BotConsoleFormatter(new TestMonitor(new BotLoggerOptions()));
        var state = new List<KeyValuePair<string, object?>>
        {
            new("BOT_TOKEN", "secret"),
            new("jwt", "aaa.bbb.ccc")
        };
        var entry = new LogEntry<List<KeyValuePair<string, object?>>>(LogLevel.Information,
            "cat",
            new EventId(0),
            state,
            null,
            (s, e) => "msg");
        using var sw = new StringWriter();
        formatter.Write(entry, null, sw);
        var output = sw.ToString();
        output.Should().Contain("BOT_TOKEN=***");
        output.Should().Contain("jwt=***");
    }

    /// <summary>
    ///     Тест 3: Сообщение не пишется при нулевой доле сэмплирования
    /// </summary>
    [Fact(DisplayName = "Тест 3: Сообщение не пишется при нулевой доле сэмплирования")]
    public void Should_Skip_When_SampledOut()
    {
        var opts = new BotLoggerOptions();
        opts.Sampling["noise"] = 0;
        var formatter = new BotConsoleFormatter(new TestMonitor(opts));
        var entry = new LogEntry<string>(LogLevel.Information,
            "cat",
            new EventId(0),
            string.Empty,
            null,
            (s, e) => "noise");
        using var sw = new StringWriter();
        formatter.Write(entry, null, sw);
        sw.ToString().Should().BeEmpty();
    }

    private sealed class TestMonitor : IOptionsMonitor<BotLoggerOptions>
    {
        public TestMonitor(BotLoggerOptions value)
        {
            CurrentValue = value;
        }

        public BotLoggerOptions CurrentValue { get; private set; }

        public BotLoggerOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<BotLoggerOptions, string> listener)
        {
            return NullDisposable.Instance;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

