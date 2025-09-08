using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Проба хранилища.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет операции чтения и записи.</item>
///     </list>
/// </remarks>
internal sealed class StorageProbe(IStateStore store) : IHealthProbe
{
    /// <summary>
    ///     Проверить хранилище.
    /// </summary>
    public async Task ProbeAsync(CancellationToken ct)
    {
        await store.SetAsync("health", "probe", 0, TimeSpan.FromSeconds(1), ct);
        await store.GetAsync<int>("health", "probe", ct);
    }
}
