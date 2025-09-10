using System.Threading.Tasks;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="CommandParsingMiddleware" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Парсинг команды и аргументов</item>
///         <item>Проброс исключений из следующего обработчика</item>
///     </list>
/// </remarks>
public sealed class CommandParsingMiddlewareTests
{
    /// <inheritdoc />
    public CommandParsingMiddlewareTests()
    {
    }

    /// <summary>
    ///     Тест 1: Middleware парсит команду и аргументы
    /// </summary>
    [Fact(DisplayName = "Тест 1: Middleware парсит команду и аргументы")]
    public async Task Should_ParseCommand_AndArgs_When_TextContainsCommand()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var mw = new CommandParsingMiddleware();
        UpdateContext? forwarded = null;
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "/test \"arg one\" arg2",
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);

        // Act
        await mw.InvokeAsync(ctx, c =>
        {
            forwarded = c;
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        // Assert
        forwarded!.Command.Should().Be("test");
        forwarded.Payload.Should().Be("\"arg one\" arg2");
        forwarded.Args.Should().Equal("arg one", "arg2");
    }

    /// <summary>
    ///     Тест 2: Исключение следующего обработчика пробрасывается
    /// </summary>
    [Fact(DisplayName = "Тест 2: Исключение следующего обработчика пробрасывается")]
    public async Task Should_PropagateException_FromNext()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var mw = new CommandParsingMiddleware();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "/fail",
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);

        // Act
        var act = async () =>
        {
            await mw.InvokeAsync(ctx, _ => throw new InvalidOperationException()).ConfigureAwait(false);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
