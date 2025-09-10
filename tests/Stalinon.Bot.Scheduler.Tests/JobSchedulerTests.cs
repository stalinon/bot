using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Scheduler.Tests;

/// <summary>
///     Тесты планировщика задач.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется запуск по интервалу</item>
///         <item>Проверяется лидер-лок</item>
///         <item>Проверяется отмена выполнения</item>
///         <item>Проверяется повторный запуск</item>
///         <item>Проверяется освобождение лока при исключении</item>
///     </list>
/// </remarks>
public sealed class JobSchedulerTests
{
    /// <inheritdoc />
    public JobSchedulerTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен запускать задачу по интервалу.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен запускать задачу по интервалу")]
    public async Task Should_RunJob_OnInterval()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IntervalJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(IntervalJob), null, TimeSpan.FromMilliseconds(50)) };
        var lockService = new FakeDistributedLock();
        var scheduler = new FakeJobScheduler(provider, jobs, lockService);

        await scheduler.RunAsync(CancellationToken.None);
        await scheduler.RunAsync(CancellationToken.None);

        var job = provider.GetRequiredService<IntervalJob>();
        job.Counter.Should().Be(2);
    }

    /// <summary>
    ///     Тест 2: Должен предотвращать параллельный запуск.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен предотвращать параллельный запуск")]
    public async Task Should_PreventParallelExecution()
    {
        var lockService = new FakeDistributedLock();

        var services1 = new ServiceCollection();
        services1.AddSingleton<LongJob>();
        var provider1 = services1.BuildServiceProvider();
        var job = provider1.GetRequiredService<LongJob>();
        var jobs = new[] { new JobDescriptor(typeof(LongJob), null, TimeSpan.FromMilliseconds(50)) };
        var scheduler1 = new FakeJobScheduler(provider1, jobs, lockService);

        var services2 = new ServiceCollection();
        services2.AddSingleton(job);
        var provider2 = services2.BuildServiceProvider();
        var scheduler2 = new FakeJobScheduler(provider2, jobs, lockService);

        var t1 = scheduler1.RunAsync(CancellationToken.None);
        var t2 = scheduler2.RunAsync(CancellationToken.None);
        await Task.WhenAll(t1, t2);

        job.Counter.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Должен прекращать выполнение при отмене.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен прекращать выполнение при отмене")]
    public async Task Should_StopExecution_WhenCancelled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IntervalJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(IntervalJob), null, TimeSpan.FromMilliseconds(50)) };
        var scheduler = new FakeJobScheduler(provider, jobs, new FakeDistributedLock());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await scheduler.Invoking(x => x.RunAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        var job = provider.GetRequiredService<IntervalJob>();
        job.Counter.Should().Be(0);
    }

    /// <summary>
    ///     Тест 4: Должен перезапускать задачи после отмены.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен перезапускать задачи после отмены")]
    public async Task Should_RestartJobs_AfterCancellation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IntervalJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(IntervalJob), null, TimeSpan.FromMilliseconds(50)) };
        var scheduler = new FakeJobScheduler(provider, jobs, new FakeDistributedLock());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await scheduler.Invoking(x => x.RunAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await scheduler.RunAsync(CancellationToken.None);

        var job = provider.GetRequiredService<IntervalJob>();
        job.Counter.Should().Be(1);
    }

    /// <summary>
    ///     Тест 5: Должен освобождать лок при исключении.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен освобождать лок при исключении")]
    public async Task Should_ReleaseLock_OnException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FlakyJob>();
        var provider = services.BuildServiceProvider();
        var jobs = new[] { new JobDescriptor(typeof(FlakyJob), null, TimeSpan.FromMilliseconds(50)) };
        var scheduler = new FakeJobScheduler(provider, jobs, new FakeDistributedLock());

        await scheduler.Invoking(x => x.RunAsync(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        await scheduler.RunAsync(CancellationToken.None);

        var job = provider.GetRequiredService<FlakyJob>();
        job.Counter.Should().Be(1);
    }
}
