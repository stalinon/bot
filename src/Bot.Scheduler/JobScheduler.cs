using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions.Contracts;

using Cronos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bot.Scheduler;

/// <summary>
///     Планировщик фоновых задач.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Поддерживает расписание по cron и интервалам</item>
///         <item>Использует лидер-лок для предотвращения параллельного выполнения</item>
///     </list>
/// </remarks>
public sealed class JobScheduler : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly IEnumerable<JobDescriptor> _jobs;
    private readonly IStateStore _store;

    /// <summary>
    ///     Создать планировщик.
    /// </summary>
    public JobScheduler(IServiceProvider provider, IEnumerable<JobDescriptor> jobs, IStateStore store)
    {
        _provider = provider;
        _jobs = jobs;
        _store = store;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();
        foreach (var job in _jobs)
        {
            tasks.Add(RunJobAsync(job, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunJobAsync(JobDescriptor descriptor, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var delay = descriptor.Interval ?? GetCronDelay(descriptor.Cron!, DateTime.UtcNow);
            try
            {
                await Task.Delay(delay, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            var key = descriptor.JobType.FullName!;
            var ttl = delay;
            var acquired = await _store.SetIfNotExistsAsync("jobs", key, 1, ttl, ct).ConfigureAwait(false);
            if (!acquired)
            {
                continue;
            }

            try
            {
                using var scope = _provider.CreateScope();
                var job = (IJob)scope.ServiceProvider.GetRequiredService(descriptor.JobType);
                await job.ExecuteAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                await _store.RemoveAsync("jobs", key, ct).ConfigureAwait(false);
            }
        }
    }

    private static TimeSpan GetCronDelay(string cron, DateTime now)
    {
        var expr = CronExpression.Parse(cron);
        var next = expr.GetNextOccurrence(now) ?? now;
        return next - now;
    }
}
