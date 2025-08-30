using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Tests;

/// <summary>
///     Провайдер логов для тестов.
/// </summary>
public sealed class CollectingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentBag<LogEntry> _logs = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <summary>
    ///     Собранные записи логов.
    /// </summary>
    public IEnumerable<LogEntry> Logs => _logs;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new Logger(_logs, _scopeProvider);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    private sealed class Logger : ILogger
    {
        private readonly ConcurrentBag<LogEntry> _logs;
        private readonly IExternalScopeProvider _scopes;

        public Logger(ConcurrentBag<LogEntry> logs, IExternalScopeProvider scopes)
        {
            _logs = logs;
            _scopes = scopes;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => _scopes.Push(state);

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopeValues = new Dictionary<string, object?>();
            _scopes.ForEachScope((scope, dict) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                {
                    foreach (var (key, value) in kvps)
                    {
                        dict[key] = value;
                    }
                }
            }, scopeValues);

            _logs.Add(new LogEntry(logLevel, formatter(state, exception), scopeValues));
        }
    }

    /// <summary>
    ///     Запись лога.
    /// </summary>
    public sealed record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Scope);
}
