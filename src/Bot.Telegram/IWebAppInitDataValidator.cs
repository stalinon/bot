namespace Bot.Telegram;

/// <summary>
///     Валидатор <c>initData</c> Telegram Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет подпись данных.</item>
///         <item>Проверяет срок жизни <c>auth_date</c>.</item>
///     </list>
/// </remarks>
public interface IWebAppInitDataValidator
{
    /// <summary>
    ///     Проверить корректность <c>initData</c>.
    /// </summary>
    bool TryValidate(string initData, out string? error);
}
