using Stalinon.Bot.Abstractions.Addresses;

namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Интерфейс для отправки кнопок Web App
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отправляет кнопку в меню.</item>
///         <item>Отправляет кнопку в inline-клавиатуре.</item>
///         <item>Отправляет кнопку в reply-клавиатуре.</item>
///     </list>
/// </remarks>
public interface IBotUi
{
    /// <summary>
    ///     Отправить Web App в меню
    /// </summary>
    Task SendMenuWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct);

    /// <summary>
    ///     Отправить Web App в inline-клавиатуре
    /// </summary>
    Task SendInlineWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct);

    /// <summary>
    ///     Отправить Web App в reply-клавиатуре
    /// </summary>
    Task SendReplyWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct);
}
