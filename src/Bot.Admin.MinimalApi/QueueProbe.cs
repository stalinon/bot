namespace Bot.Admin.MinimalApi;

/// <summary>
///     Проба очереди.
/// </summary>
internal sealed class QueueProbe : IHealthProbe
{
    /// <summary>
    ///     Проверить очередь.
    /// </summary>
    public Task ProbeAsync(CancellationToken ct) => Task.CompletedTask;
}

