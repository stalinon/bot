using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Pipeline;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Проверка скоупов пайплайна
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет, что создаётся ровно один scope и middleware разрешается из контекста</item>
///     </list>
/// </remarks>
public sealed class PipelineBuilderScopeTests
{
    /// <inheritdoc />
    public PipelineBuilderScopeTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать ровно один scope и разрешать middleware из контекста
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать ровно один scope и разрешать middleware из контекста")]
    public async Task Should_CreateSingleScope_AndResolveMiddlewareFromContext()
    {
        // Arrange
        DummyMiddleware.ProviderMatches = false;
        var services = new ServiceCollection();
        services.AddScoped<DummyMiddleware>();
        var root = services.BuildServiceProvider();
        var countingFactory = new CountingServiceScopeFactory(root.GetRequiredService<IServiceScopeFactory>());
        var builder = new PipelineBuilder(countingFactory);
        builder.Use<DummyMiddleware>();
        var app = builder.Build(_ => Task.CompletedTask);
        var ctx = CreateContext(root);

        // Act
        await app(ctx);

        // Assert
        countingFactory.Count.Should().Be(1);
        DummyMiddleware.ProviderMatches.Should().BeTrue();
    }

    private static UpdateContext CreateContext(IServiceProvider sp)
    {
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
            sp,
            CancellationToken.None);
    }

    private sealed class DummyMiddleware(IServiceProvider sp) : IUpdateMiddleware
    {
        public static bool ProviderMatches;

        public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
        {
            ProviderMatches = ReferenceEquals(sp, ctx.Services);
            return next(ctx);
        }
    }

    private sealed class CountingServiceScopeFactory(IServiceScopeFactory inner) : IServiceScopeFactory
    {
        public int Count { get; private set; }

        public IServiceScope CreateScope()
        {
            Count++;
            return inner.CreateScope();
        }
    }
}

