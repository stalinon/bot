namespace Stalinon.Bot.Hosting.Options;

/// <summary>
///     Настройки аутбокса.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет путь хранения сообщений.</item>
///     </list>
/// </remarks>
public sealed class OutboxOptions
{
    /// <summary>
    ///     Путь к каталогу аутбокса.
    /// </summary>
    public string Path { get; set; } = "outbox";
}

