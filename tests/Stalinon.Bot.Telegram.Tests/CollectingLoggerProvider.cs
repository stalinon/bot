using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Stalinon.Bot.Telegram.Tests;

/// <summary>
///     Провайдер логов для тестов.
/// </summary>
public sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<LogEntry> _logs = new();

    /// <summary>
    ///     Собранные записи логов.
    /// </summary>
    public IEnumerable<LogEntry> Logs => _logs;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(_logs);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class Logger : ILogger
    {
        private readonly ConcurrentBag<LogEntry> _logs;

        public Logger(ConcurrentBag<LogEntry> logs)
        {
            _logs = logs;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _logs.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     Запись лога.
    /// </summary>
    public sealed record LogEntry(LogLevel Level, string Message);
}
