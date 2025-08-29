using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="LoggingMiddleware"/>.
/// </summary>
public class LoggingMiddlewareTests
{
    /// <summary>
    ///     В логах присутствуют тип обновления, идентификаторы сообщения и пользователя.
    /// </summary>
    [Fact(DisplayName = "Тест 1. В логах присутствуют тип обновления, идентификаторы сообщения и пользователя")]
    public async Task Log_contains_update_type_message_and_user_ids()
    {
        var logger = new ListLogger<LoggingMiddleware>();
        var mw = new LoggingMiddleware(logger);
        var ctx = new UpdateContext(
            Transport: "test",
            UpdateId: "42",
            Chat: new ChatAddress(1),
            User: new UserAddress(123),
            Text: null,
            Command: null,
            Args: null,
            Payload: null,
            Items: new Dictionary<string, object>
            {
                ["UpdateType"] = "Message",
                ["MessageId"] = 7
            },
            Services: new DummyServiceProvider(),
            CancellationToken: CancellationToken.None);

        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);

        Assert.Contains(logger.Logs, s => s.Contains("Message") && s.Contains('7') && s.Contains("123"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Logs { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Logs.Add(formatter(state, exception));
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
