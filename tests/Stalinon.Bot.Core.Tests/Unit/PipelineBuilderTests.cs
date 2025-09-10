using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Core.Pipeline;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Набор тестов для <see cref="PipelineBuilder" />
/// </summary>
public class PipelineBuilderTests
{
    /// <summary>
    ///     1. Проверяет, что после вызова <see cref="PipelineBuilder.Build" /> метод
    ///     <see cref="PipelineBuilder.Use(System.Func{Stalinon.Bot.Abstractions.UpdateDelegate,Stalinon.Bot.Abstractions.UpdateDelegate})" />
    ///     выбрасывает исключение
    /// </summary>
    [Fact(DisplayName = "Тест 1. Use после Build выбрасывает исключение")]
    public void Use_after_build_throws()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());

        builder.Build(_ => ValueTask.CompletedTask);

        Assert.Throws<InvalidOperationException>(() => builder.Use(next => next));
    }

    /// <summary>
    ///     2. Проверяет потокобезопасность регистрации middleware в методе
    ///     <see cref="PipelineBuilder.Use(System.Func{Stalinon.Bot.Abstractions.UpdateDelegate,Stalinon.Bot.Abstractions.UpdateDelegate})" />
    /// </summary>
    [Fact(DisplayName = "Тест 2. Use потокобезопасен")]
    public async Task Use_is_thread_safe()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new PipelineBuilder(services.GetRequiredService<IServiceScopeFactory>());

        var counter = 0;
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            builder.Use(next => async ctx =>
            {
                Interlocked.Increment(ref counter);
                await next(ctx).ConfigureAwait(false);
            })));

        await Task.WhenAll(tasks);

        var app = builder.Build(_ => ValueTask.CompletedTask);
        await app(CreateContext()).ConfigureAwait(false);

        Assert.Equal(50, counter);
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
