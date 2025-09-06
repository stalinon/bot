namespace Bot.Hosting.Options;

/// <summary>
///     Настройки наблюдаемости.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Включение экспортёров метрик и трассировок.</item>
///     </list>
/// </remarks>
public sealed class ObservabilityOptions
{
    /// <summary>
    ///     Параметры экспортёров.
    /// </summary>
    public ObservabilityExportOptions Export { get; set; } = new();
}

