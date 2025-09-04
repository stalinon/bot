using System;
using System.Threading;
using System.Threading.Tasks;

using Bot.Scheduler;

namespace Bot.Scheduler.Tests;

/// <summary>
///     Долгая задача.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Используется для проверки лидер-лока</item>
///         <item>Задерживается на заданное время</item>
///     </list>
/// </remarks>
internal sealed class LongJob : IJob
{
    private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(20);
    private int _counter;

    /// <summary>
    ///     Количество запусков.
    /// </summary>
    public int Counter => _counter;

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _counter);
        await Task.Delay(_delay, ct);
    }
}
