using System.Threading.Tasks;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Core.Pipeline;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Проверка исполнения цепочек <see cref="PipelineBuilder" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Средства вызываются в указанном порядке</item>
///         <item>Исключение прерывает дальнейшее выполнение</item>
///     </list>
/// </remarks>
public sealed class PipelineExecutionTests
{
    /// <inheritdoc />
    public PipelineExecutionTests()
    {
    }

    /// <summary>
    ///     Тест 1: Middleware выполняются последовательно
    /// </summary>
    [Fact(DisplayName = "Тест 1: Middleware выполняются последовательно")]
    public async Task Should_InvokeMiddlewares_InRegisteredOrder()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());
        var order = new List<string>();
        builder.Use(next => async ctx =>
        {
            order.Add("first");
            await next(ctx).ConfigureAwait(false);
        });
        builder.Use(next => async ctx =>
        {
            order.Add("second");
            await next(ctx).ConfigureAwait(false);
        });
        var app = builder.Build(_ =>
        {
            order.Add("terminal");
            return ValueTask.CompletedTask;
        });
        var ctx = CreateContext();

        // Act
        await app(ctx).ConfigureAwait(false);

        // Assert
        order.Should().Equal("first", "second", "terminal");
    }

    /// <summary>
    ///     Тест 2: Исключение прерывает цепочку
    /// </summary>
    [Fact(DisplayName = "Тест 2: Исключение прерывает цепочку")]
    public async Task Should_StopPipeline_When_MiddlewareThrows()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());
        var called = false;
        builder.Use(next => _ => throw new InvalidOperationException());
        builder.Use(next => ctx =>
        {
            called = true;
            return next(ctx);
        });
        var app = builder.Build(_ => ValueTask.CompletedTask);
        var ctx = CreateContext();

        // Act
        var act = async () => await app(ctx).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        called.Should().BeFalse();
    }

    private static UpdateContext CreateContext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);
    }
}
