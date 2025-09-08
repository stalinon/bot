using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Scheduler;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты фейковых компонентов TestKit.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется TTL распределённого лока</item>
///         <item>Проверяется запуск задачи планировщиком</item>
///     </list>
/// </remarks>
public sealed class FakeComponentsTests
{
    /// <inheritdoc />
    public FakeComponentsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Лок должен освобождаться по TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Лок должен освобождаться по TTL")]
    public async Task Should_ReleaseLock_ByTtl()
    {
        var @lock = new FakeDistributedLock();
        var first = await @lock.AcquireAsync("key", TimeSpan.FromMilliseconds(20), CancellationToken.None);
        var second = await @lock.AcquireAsync("key", TimeSpan.FromMilliseconds(20), CancellationToken.None);
        first.Should().BeTrue();
        second.Should().BeFalse();
        await Task.Delay(40);
        var third = await @lock.AcquireAsync("key", TimeSpan.FromMilliseconds(20), CancellationToken.None);
        third.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 2: Планировщик должен запускать задачу.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Планировщик должен запускать задачу")]
    public async Task Should_RunJob()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DummyJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(DummyJob), null, TimeSpan.FromMilliseconds(10)) };
        var scheduler = new FakeJobScheduler(provider, jobs, new FakeDistributedLock());

        await scheduler.RunAsync(CancellationToken.None);

        var job = provider.GetRequiredService<DummyJob>();
        job.Counter.Should().Be(1);
    }

    private sealed class DummyJob : IJob
    {
        private int _counter;
        public int Counter => _counter;

        public Task ExecuteAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _counter);
            return Task.CompletedTask;
        }
    }
}
