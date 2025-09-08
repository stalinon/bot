namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Отправитель ответа на запрос Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет контракт ответа на запрос Web App.</item>
///         <item>Реализации обязаны обеспечивать идемпотентность по <c>query_id</c>.</item>
///     </list>
/// </remarks>
public interface IWebAppQueryResponder
{
    /// <summary>
    ///     Ответить на запрос Web App текстом.
    /// </summary>
    Task<bool> RespondAsync(string queryId, string text, CancellationToken ct);

    /// <summary>
    ///     Ответить на запрос Web App изображением.
    /// </summary>
    Task<bool> RespondWithImageAsync(string queryId, string url, CancellationToken ct);

    /// <summary>
    ///     Ответить на запрос Web App текстом с кнопкой.
    /// </summary>
    Task<bool> RespondWithButtonAsync(string queryId, string text, string buttonText, string buttonUrl,
        CancellationToken ct);
}
