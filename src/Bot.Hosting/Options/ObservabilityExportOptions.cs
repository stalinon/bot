namespace Bot.Hosting.Options;

/// <summary>
///     Настройки экспортёров наблюдаемости.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Включает OTLP.</item>
///         <item>Включает Prometheus.</item>
///     </list>
/// </remarks>
public sealed class ObservabilityExportOptions
{
    /// <summary>
    ///     Включить OTLP.
    /// </summary>
    public bool Otlp { get; set; }

    /// <summary>
    ///     Включить Prometheus.
    /// </summary>
    public bool Prometheus { get; set; }
}

