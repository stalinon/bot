namespace Bot.Scheduler.Tests;

/// <summary>
///     Задача с подсчётом запусков.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Используется в тестах</item>
///         <item>Увеличивает счётчик при выполнении</item>
///     </list>
/// </remarks>
internal sealed class IntervalJob : IJob
{
    private int _counter;

    /// <summary>
    ///     Количество запусков.
    /// </summary>
    public int Counter => _counter;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _counter);
        return Task.CompletedTask;
    }
}
