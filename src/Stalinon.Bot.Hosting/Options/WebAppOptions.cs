namespace Stalinon.Bot.Hosting.Options;

/// <summary>
///     Настройки Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Содержит публичный URL мини-приложения.</item>
///         <item>Определяет время жизни токена авторизации.</item>
///         <item>Определяет время жизни параметра <c>initData</c>.</item>
///         <item>Хранит параметры политики безопасности.</item>
///     </list>
/// </remarks>
public sealed class WebAppOptions
{
    /// <summary>
    ///     Публичный URL мини-приложения.
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Время жизни токена авторизации в секундах.
    /// </summary>
    public int AuthTtlSeconds { get; set; } = 300;

    /// <summary>
    ///     Время жизни <c>initData</c> в секундах.
    /// </summary>
    public int InitDataTtlSeconds { get; set; } = 300;

    /// <summary>
    ///     Параметры Content-Security-Policy.
    /// </summary>
    public WebAppCspOptions Csp { get; set; } = new();
}
