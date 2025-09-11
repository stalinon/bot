using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Scheduler;

using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты FakeJobScheduler: запуск задач и пропуск при захваченном локе.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется выполнение задачи</item>
///         <item>Проверяется пропуск задачи при захваченном локе</item>
///     </list>
/// </remarks>
public sealed class FakeJobSchedulerTests
{
    /// <inheritdoc/>
    public FakeJobSchedulerTests()
    {
    }

    /// <summary>
    ///     Тест 1: Планировщик должен запускать задачу.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Планировщик должен запускать задачу")]
    public async Task Should_RunJob()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<DummyJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(DummyJob), null, TimeSpan.FromMilliseconds(10)) };
        var scheduler = new FakeJobScheduler(provider, jobs, new FakeDistributedLock());

        // Act
        await scheduler.RunAsync(CancellationToken.None);

        // Assert
        var job = provider.GetRequiredService<DummyJob>();
        job.Counter.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: Планировщик должен пропускать задачу при захваченном локе.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Планировщик должен пропускать задачу при захваченном локе")]
    public async Task Should_SkipJob_WhenLockHeld()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<DummyJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(DummyJob), null, TimeSpan.FromMilliseconds(10)) };
        var @lock = new FakeDistributedLock();
        await @lock.AcquireAsync(typeof(DummyJob).FullName!, TimeSpan.FromSeconds(1), CancellationToken.None);
        var scheduler = new FakeJobScheduler(provider, jobs, @lock);

        // Act
        await scheduler.RunAsync(CancellationToken.None);

        // Assert
        var job = provider.GetRequiredService<DummyJob>();
        job.Counter.Should().Be(0);
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

