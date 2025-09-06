using System.Diagnostics;

namespace Bot.Observability;

/// <summary>
///     Источник активности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Используется для построения спанов в различных компонентах.</item>
///     </list>
/// </remarks>
public static class Telemetry
{
    /// <summary>
    ///     Имя источника активности.
    /// </summary>
    public const string ActivitySourceName = "Bot";

    /// <summary>
    ///     Глобальный источник активности приложения.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
