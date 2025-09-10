using FluentAssertions;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты SceneTestExtensions: создание контекста и переходы.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется создание контекста.</item>
///         <item>Проверяется переход между шагами и TTL.</item>
///     </list>
/// </remarks>
public sealed class SceneTestExtensionsTests
{
    /// <inheritdoc />
    public SceneTestExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать контекст с указанными параметрами.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать контекст с указанными параметрами")]
    public void Should_CreateContext_WithValues()
    {
        var ctx = SceneTestExtensions.CreateContext("text", "cmd");

        ctx.Transport.Should().Be("tg");
        ctx.Text.Should().Be("text");
        ctx.Command.Should().Be("cmd");
        ctx.Chat.Should().Be(new ChatAddress(1));
        ctx.User.Should().Be(new UserAddress(1));
        ctx.Services.Should().NotBeNull();
    }

    /// <summary>
    ///     Тест 2: Должен переходить между шагами и сбрасывать состояние по TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен переходить между шагами и сбрасывать состояние по TTL")]
    public async Task Should_TransitAndExpire()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMilliseconds(100));
        var handler = new DummyHandler(navigator);
        var ctx = SceneTestExtensions.CreateContext("/go", "go");
        await handler.HandleAsync(ctx);

        var state = await handler.StepAsync(navigator, ctx, "next");
        var expired = await handler.StepAsync(navigator, ctx, "next", delay: TimeSpan.FromMilliseconds(150));

        state.Should().NotBeNull();
        state!.Step.Should().Be(1);
        expired.Should().BeNull();
    }

    private sealed class DummyHandler(SceneNavigator navigator) : IUpdateHandler
    {
        private readonly SceneNavigator _navigator = navigator;
        private readonly DummyScene _scene = new();

        public async Task HandleAsync(UpdateContext ctx)
        {
            if (ctx.Command == "go")
            {
                await _navigator.EnterAsync(ctx, _scene);
                return;
            }

            var state = await _navigator.GetStateAsync(ctx);
            if (state is null)
            {
                return;
            }

            await _navigator.NextStepAsync(ctx);
        }

        private sealed class DummyScene : IScene
        {
            public string Name => "dummy";

            public Task<bool> CanEnter(UpdateContext ctx)
            {
                return Task.FromResult(true);
            }

            public Task OnEnter(UpdateContext ctx)
            {
                return Task.CompletedTask;
            }

            public Task OnUpdate(UpdateContext ctx)
            {
                return Task.CompletedTask;
            }
        }
    }
}

