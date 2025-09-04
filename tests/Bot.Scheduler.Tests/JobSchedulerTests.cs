using System;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions.Contracts;
using Bot.Hosting;
using Bot.Scheduler;
using Bot.TestKit;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace Bot.Scheduler.Tests;

/// <summary>
///     Тесты планировщика задач.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется запуск по интервалу</item>
///         <item>Проверяется лидер-лок</item>
///     </list>
/// </remarks>
public sealed class JobSchedulerTests
{
    /// <inheritdoc/>
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
        services.AddSingleton<IStateStore, InMemoryStateStore>();
        services.AddSingleton<IntervalJob>();
        services.AddJob<IntervalJob>(interval: TimeSpan.FromMilliseconds(50));
        services.AddJobScheduler();
        var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IHostedService>();

        await scheduler.StartAsync(CancellationToken.None);
        await Task.Delay(160);
        await scheduler.StopAsync(CancellationToken.None);

        var job = provider.GetRequiredService<IntervalJob>();
        job.Counter.Should().BeGreaterOrEqualTo(2);
    }

    /// <summary>
    ///     Тест 2: Должен предотвращать параллельный запуск.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен предотвращать параллельный запуск")]
    public async Task Should_PreventParallelExecution()
    {
        var store = new AtomicStateStore();

        var services1 = new ServiceCollection();
        services1.AddSingleton<IStateStore>(store);
        services1.AddSingleton<LongJob>();
        services1.AddJob<LongJob>(interval: TimeSpan.FromMilliseconds(50));
        services1.AddJobScheduler();
        var provider1 = services1.BuildServiceProvider();
        var scheduler1 = provider1.GetRequiredService<IHostedService>();

        var services2 = new ServiceCollection();
        services2.AddSingleton<IStateStore>(store);
        services2.AddSingleton(provider1.GetRequiredService<LongJob>());
        services2.AddJob<LongJob>(interval: TimeSpan.FromMilliseconds(50));
        services2.AddJobScheduler();
        var provider2 = services2.BuildServiceProvider();
        var scheduler2 = provider2.GetRequiredService<IHostedService>();

        await scheduler1.StartAsync(CancellationToken.None);
        await scheduler2.StartAsync(CancellationToken.None);
        await Task.Delay(80);
        await scheduler1.StopAsync(CancellationToken.None);
        await scheduler2.StopAsync(CancellationToken.None);

        var job = provider1.GetRequiredService<LongJob>();
        job.Counter.Should().Be(1);
    }
}
