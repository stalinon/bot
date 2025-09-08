namespace Stalinon.Bot.Hosting.Options;

/// <summary>
///     Параметры Content-Security-Policy для Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет список разрешённых origin'ов.</item>
///     </list>
/// </remarks>
public sealed class WebAppCspOptions
{
    /// <summary>
    ///     Разрешённые origin'ы.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
