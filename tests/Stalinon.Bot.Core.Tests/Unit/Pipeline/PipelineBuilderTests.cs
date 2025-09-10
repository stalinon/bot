using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Core.Pipeline;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="PipelineBuilder" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запрет регистрации после построения цепочки</item>
///         <item>Потокобезопасность регистрации</item>
///     </list>
/// </remarks>
public sealed class PipelineBuilderTests
{
    /// <inheritdoc />
    public PipelineBuilderTests()
    {
    }

    /// <summary>
    ///     Тест 1: Use после Build выбрасывает исключение
    /// </summary>
    [Fact(DisplayName = "Тест 1: Use после Build выбрасывает исключение")]
    public void Should_Throw_When_UseCalled_AfterBuild()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());
        builder.Build(_ => ValueTask.CompletedTask);

        // Act
        var act = () => builder.Use(next => next);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    ///     Тест 2: Регистрация middleware потокобезопасна
    /// </summary>
    [Fact(DisplayName = "Тест 2: Регистрация middleware потокобезопасна")]
    public async Task Should_RegisterMiddlewares_When_UsedConcurrently()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());
        var counter = 0;
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            builder.Use(next => async ctx =>
            {
                Interlocked.Increment(ref counter);
                await next(ctx).ConfigureAwait(false);
            })));

        // Act
        await Task.WhenAll(tasks);
        var app = builder.Build(_ => ValueTask.CompletedTask);
        await app(CreateContext()).ConfigureAwait(false);

        // Assert
        counter.Should().Be(50);
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
