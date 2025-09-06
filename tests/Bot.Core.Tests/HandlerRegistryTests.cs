using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Core.Routing;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Набор тестов для <see cref="HandlerRegistry" />
/// </summary>
public class HandlerRegistryTests
{
    private static UpdateContext CreateContext(string? text, string? command, string[]? args = null)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            text,
            command,
            args,
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

        var ctx = CreateContext("/cmd", "cmd");
        registry.FindFor(ctx).Should().Be(typeof(CmdHandler));
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
        registry.FindFor(ctx).Should().Be(typeof(SecondRegexHandler));
    }

    /// <summary>
    ///     3. Проверяет привязку аргументов к типу
    /// </summary>
    [Fact(DisplayName = "Тест 3. Привязка аргументов к типу")]
    public void Should_Bind_Arguments()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(VoteHandler));

        var ctx = CreateContext("/vote tax 5", "vote", ["tax", "5"]);
        registry.FindFor(ctx).Should().Be(typeof(VoteHandler));
        ctx.GetItem<VoteArgs>(UpdateItems.CommandArgs)!
            .Should().BeEquivalentTo(new VoteArgs("tax", 5));
    }

    /// <summary>
    ///     4. Проверяет валидацию аргументов
    /// </summary>
    [Fact(DisplayName = "Тест 4. Валидация аргументов")]
    public void Should_Validate_Arguments()
    {
        var registry = new HandlerRegistry();
        registry.Register(typeof(VoteHandler));

        var ctx = CreateContext("/vote tax 50", "vote", ["tax", "50"]);
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

    [TextMatch(".*")]
    private sealed class FirstRegexHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }

    [TextMatch(".*")]
    private sealed class SecondRegexHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }

    [Command("vote", typeof(VoteArgs))]
    private sealed class VoteHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private sealed record VoteArgs(string Target, [property: Range(1, 10)] int Value);
}
