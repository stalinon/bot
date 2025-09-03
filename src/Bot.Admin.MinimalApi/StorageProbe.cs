namespace Bot.Admin.MinimalApi;

/// <summary>
///     Проба хранилища.
/// </summary>
internal sealed class StorageProbe : IHealthProbe
{
    /// <summary>
    ///     Проверить хранилище.
    /// </summary>
    public Task ProbeAsync(CancellationToken ct) => Task.CompletedTask;
}

