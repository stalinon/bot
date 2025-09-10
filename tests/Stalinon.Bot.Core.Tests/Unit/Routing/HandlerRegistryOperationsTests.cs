using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Routing;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты регистра обработчиков.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется регистрация обработчика.</item>
///         <item>Проверяется поиск обработчика.</item>
///         <item>Проверяется удаление обработчика.</item>
///     </list>
/// </remarks>
public sealed class HandlerRegistryOperationsTests
{
    private static UpdateContext CreateContext(string? text, string? command)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            text,
            command,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);
    }

    /// <summary>
    ///     Тест 1: Регистрация добавляет обработчик для команды.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрация добавляет обработчик для команды.")]
    public void Should_Register_Handler_ForCommand()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(CmdHandler));
        var ctx = CreateContext("/cmd", "cmd");

        registry.FindFor(ctx).Should().Be(typeof(CmdHandler));
    }

    /// <summary>
    ///     Тест 2: Поиск возвращает null для неизвестной команды.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Поиск возвращает null для неизвестной команды.")]
    public void Should_ReturnNull_When_HandlerNotFound()
    {
        var registry = new HandlerRegistry();
        var ctx = CreateContext("/unknown", "unknown");

        registry.FindFor(ctx).Should().BeNull();
    }

    /// <summary>
    ///     Тест 3: Удаление исключает обработчик из поиска.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Удаление исключает обработчик из поиска.")]
    public void Should_Remove_Handler_FromRegistry()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(CmdHandler));
        var ctx = CreateContext("/cmd", "cmd");
        registry.FindFor(ctx).Should().Be(typeof(CmdHandler));

        registry.Remove(typeof(CmdHandler));
        registry.FindFor(ctx).Should().BeNull();
    }

    [Command("cmd")]
    private sealed class CmdHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }
}

