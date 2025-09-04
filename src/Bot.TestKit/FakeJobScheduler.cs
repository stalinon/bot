using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions.Contracts;
using Bot.Scheduler;

using Microsoft.Extensions.DependencyInjection;

namespace Bot.TestKit;

/// <summary>
///     Упрощённый планировщик задач для тестов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запускает задачи по требованию без фоновых потоков</item>
///         <item>Использует распределённый лок для предотвращения параллельного выполнения</item>
///     </list>
/// </remarks>
public sealed class FakeJobScheduler
{
    private readonly IServiceProvider _provider;
    private readonly IEnumerable<JobDescriptor> _jobs;
    private readonly IDistributedLock _lock;

    /// <summary>
    ///     Создать планировщик.
    /// </summary>
    public FakeJobScheduler(IServiceProvider provider, IEnumerable<JobDescriptor> jobs, IDistributedLock @lock)
    {
        _provider = provider;
        _jobs = jobs;
        _lock = @lock;
    }

    /// <summary>
    ///     Запустить все задачи один раз.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        foreach (var descriptor in _jobs)
        {
            ct.ThrowIfCancellationRequested();
            var key = descriptor.JobType.FullName!;
            var ttl = descriptor.Interval ?? TimeSpan.Zero;
            var acquired = await _lock.AcquireAsync(key, ttl, ct).ConfigureAwait(false);
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
                await _lock.ReleaseAsync(key, ct).ConfigureAwait(false);
            }
        }
    }
}
