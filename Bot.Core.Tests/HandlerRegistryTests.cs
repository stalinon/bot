using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Core.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Набор тестов для <see cref="HandlerRegistry" />
/// </summary>
public class HandlerRegistryTests
{
    [Command("/cmd")]
    private sealed class CmdHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx) => Task.CompletedTask;
    }

    [TextMatch(".*")]
    private sealed class FirstRegexHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx) => Task.CompletedTask;
    }

    [TextMatch(".*")]
    private sealed class SecondRegexHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx) => Task.CompletedTask;
    }

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
    ///     1. Проверяет, что команда имеет приоритет над регулярным выражением
    /// </summary>
    [Fact(DisplayName = "Тест 1. Команда имеет приоритет над регулярным выражением")]
    public void Command_priority_over_regex()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(CmdHandler));
        registry.Register(typeof(FirstRegexHandler));

        var ctx = CreateContext("/cmd", "/cmd");
        Assert.Equal(typeof(CmdHandler), registry.FindFor(ctx));
    }

    /// <summary>
    ///     2. Проверяет, что регулярные выражения обрабатываются в порядке регистрации
    /// </summary>
    [Fact(DisplayName = "Тест 2. Регулярные выражения обрабатываются в порядке регистрации")]
    public void Regex_order_respected()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(SecondRegexHandler));
        registry.Register(typeof(FirstRegexHandler));

        var ctx = CreateContext("anything", null);
        Assert.Equal(typeof(SecondRegexHandler), registry.FindFor(ctx));
    }
}
