namespace Stalinon.Bot.Scheduler.Tests;

/// <summary>
///     Задача, которая один раз падает, затем выполняется.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Используется для проверки обработки исключений</item>
///         <item>После первого сбоя увеличивает счётчик</item>
///     </list>
/// </remarks>
internal sealed class FlakyJob : IJob
{
    private bool _failed = true;
    private int _counter;

    /// <summary>
    ///     Количество успешных запусков.
    /// </summary>
    public int Counter => _counter;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        if (_failed)
        {
            _failed = false;
            throw new InvalidOperationException("ошибка");
        }

        Interlocked.Increment(ref _counter);
        return Task.CompletedTask;
    }
}
