using System;

namespace Bot.WebApp.MinimalApi;

/// <summary>
///     Настройки авторизации Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит секрет подписи JWT.</item>
///         <item>Определяет срок действия JWT.</item>
///     </list>
/// </remarks>
public sealed class WebAppAuthOptions
{
    /// <summary>
    ///     Секрет подписи JWT.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    ///     Срок действия JWT.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(5);
}

