namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Настройки административного API.
/// </summary>
public sealed class AdminOptions
{
    /// <summary>
    ///     Токен администратора.
    /// </summary>
    public string AdminToken { get; set; } = string.Empty;
}
