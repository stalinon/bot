using System.Threading.Tasks;

using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Tests.Shared;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="ExceptionHandlingMiddleware" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Успешная передача обработки следующему звену</item>
///         <item>Логирование и подавление необработанного исключения</item>
///         <item>Игнорирование отмены по запрошенному токену</item>
///     </list>
/// </remarks>
public sealed class ExceptionHandlingMiddlewareTests
{
    /// <inheritdoc />
    public ExceptionHandlingMiddlewareTests()
    {
    }

    /// <summary>
    ///     Тест 1: Исключения отсутствуют и обработчик вызывается
    /// </summary>
    [Fact(DisplayName = "Тест 1: Исключения отсутствуют и обработчик вызывается")]
    public async Task Should_InvokeNext_When_NoException()
    {
        // Arrange
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var mw = new ExceptionHandlingMiddleware(sp.GetRequiredService<ILogger<ExceptionHandlingMiddleware>>());
        var called = false;
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
            CancellationToken.None);

        // Act
        await mw.InvokeAsync(ctx, _ =>
        {
            called = true;
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        // Assert
        called.Should().BeTrue();
        provider.Logs.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 2: Необработанное исключение логируется и не пробрасывается
    /// </summary>
    [Fact(DisplayName = "Тест 2: Необработанное исключение логируется и не пробрасывается")]
    public async Task Should_LogAndSwallow_When_NextThrows()
    {
        // Arrange
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var mw = new ExceptionHandlingMiddleware(sp.GetRequiredService<ILogger<ExceptionHandlingMiddleware>>());
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
            CancellationToken.None);

        // Act
        await mw.InvokeAsync(ctx, _ => throw new InvalidOperationException()).ConfigureAwait(false);

        // Assert
        provider.Logs.Should().Contain(e => e.Level == LogLevel.Error);
    }

    /// <summary>
    ///     Тест 3: Отмена по токену не логируется и не пробрасывается
    /// </summary>
    [Fact(DisplayName = "Тест 3: Отмена по токену не логируется и не пробрасывается")]
    public async Task Should_SwallowCancellation_When_TokenCancelled()
    {
        // Arrange
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var mw = new ExceptionHandlingMiddleware(sp.GetRequiredService<ILogger<ExceptionHandlingMiddleware>>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
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
            cts.Token);

        // Act
        await mw.InvokeAsync(ctx, _ => throw new OperationCanceledException(cts.Token)).ConfigureAwait(false);

        // Assert
        provider.Logs.Should().BeEmpty();
    }
}
