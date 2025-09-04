using Bot.Abstractions.Addresses;

namespace Bot.Telegram;

/// <summary>
///     Интерфейс управления меню чата.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Устанавливает или обновляет кнопку Web App.</item>
///         <item>Кэширует текущие настройки, избегая лишних запросов.</item>
///     </list>
/// </remarks>
public interface IChatMenuService
{
    /// <summary>
    ///     Установить кнопку Web App в меню чата.
    /// </summary>
    Task SetWebAppMenuAsync(ChatAddress chat, string text, string url, CancellationToken ct);
}
