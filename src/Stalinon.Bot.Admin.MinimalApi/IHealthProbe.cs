namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Проба готовности зависимости.
/// </summary>
public interface IHealthProbe
{
    /// <summary>
    ///     Проверить зависимость.
    /// </summary>
    Task ProbeAsync(CancellationToken ct);
}
