using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests;

/// <summary>
///     Тесты остановки фонового сервиса.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Вызывает StopAsync источника обновлений.</item>
///         <item>Учитывает потерянные обновления при остановке.</item>
///     </list>
/// </remarks>
public sealed class StopTests
{
    /// <summary>
    ///     Тест 1: Должен вызвать StopAsync у источника при остановке.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен вызвать StopAsync у источника при остановке")]
    public async Task Should_CallStop_OnSource_When_ServiceStopping()
    {
        var src = new Mock<IUpdateSource>();
        src.Setup(x => x.StartAsync(It.IsAny<Func<UpdateContext, Task>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        src.Setup(x => x.StopAsync()).Returns(Task.CompletedTask).Verifiable();

        var hosted = new BotHostedService(
            src.Object,
            new DummyPipeline(),
            [],
            new StatsCollector(),
            new LoggerFactory().CreateLogger<BotHostedService>(),
            Microsoft.Extensions.Options.Options.Create(new BotOptions()),
            Microsoft.Extensions.Options.Options.Create(new StopOptions()));

        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);

        src.Verify(x => x.StopAsync(), Times.Once);
    }

    /// <summary>
    ///     Тест 2: Должен учитывать потерянные обновления при остановке.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен учитывать потерянные обновления при остановке")]
    public async Task Should_RecordLostUpdates_When_StoppingWithPending()
    {
        var source = new TestSource();
        var stats = new StatsCollector();
        var pipeline = new TestPipeline();
        pipeline.Use(next => async ctx =>
        {
            await Task.Delay(200, ctx.CancellationToken);
            await next(ctx);
        });

        var hosted = new BotHostedService(
            source,
            pipeline,
            [],
            stats,
            new LoggerFactory().CreateLogger<BotHostedService>(),
            Microsoft.Extensions.Options.Options.Create(new BotOptions
            {
                DrainTimeout = TimeSpan.FromMilliseconds(50),
                Transport = new TransportOptions { Parallelism = 1 }
            }),
            Microsoft.Extensions.Options.Options.Create(new StopOptions()));

        await hosted.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await hosted.StopAsync(CancellationToken.None);

        var snapshot = stats.GetSnapshot();
        snapshot.LostUpdates.Should().Be(1);
    }

    private sealed class TestSource : IUpdateSource
    {
        public Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
        {
            var ctx1 = new UpdateContext(
                "test",
                "1",
                new ChatAddress(1),
                new UserAddress(1),
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                null!,
                ct);
            var ctx2 = new UpdateContext(
                "test",
                "2",
                new ChatAddress(1),
                new UserAddress(1),
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                null!,
                ct);
            _ = onUpdate(ctx1);
            _ = onUpdate(ctx2);
            var tcs = new TaskCompletionSource();
            ct.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestPipeline : IUpdatePipeline
    {
        private readonly List<Func<UpdateDelegate, UpdateDelegate>> _components = new();

        public IUpdatePipeline Use<T>() where T : IUpdateMiddleware
        {
            return this;
        }

        public IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component)
        {
            _components.Add(component);
            return this;
        }

        public UpdateDelegate Build(UpdateDelegate terminal)
        {
            var app = terminal;
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                app = _components[i](app);
            }

            return app;
        }
    }
}
