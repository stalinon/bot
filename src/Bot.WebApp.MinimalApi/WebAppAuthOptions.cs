namespace Bot.WebApp.MinimalApi;

/// <summary>
///     Настройки авторизации Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит секрет подписи JWT.</item>
///     </list>
/// </remarks>
public sealed class WebAppAuthOptions
{
    /// <summary>
    ///     Секрет подписи JWT.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}
