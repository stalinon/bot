namespace Bot.Admin.MinimalApi;

/// <summary>
///     Проба транспорта.
/// </summary>
internal sealed class TransportProbe : IHealthProbe
{
    /// <summary>
    ///     Проверить транспорт.
    /// </summary>
    public Task ProbeAsync(CancellationToken ct) => Task.CompletedTask;
}

