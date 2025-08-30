using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты логирующего middleware.
/// </summary>
public class LoggingMiddlewareTests
{
    /// <summary>
    ///     Проверяет, что логи обработчика содержат UpdateId.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Лог содержит UpdateId")]
    public async Task Log_contains_update_id()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);

        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            sp,
            default);

        UpdateDelegate next = _ =>
        {
            var handlerLogger = sp.GetRequiredService<ILogger<LoggingMiddlewareTests>>();
            handlerLogger.LogInformation("handler");
            return Task.CompletedTask;
        };

        await mw.InvokeAsync(ctx, next);

        Assert.Contains(provider.Logs, l => l.Scope.TryGetValue("UpdateId", out var id) && id?.ToString() == "1");
    }
}
