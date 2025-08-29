namespace Bot.Storage.File.Options;

/// <summary>
///     Настройки
/// </summary>
public sealed class FileStoreOptions
{
    /// <summary>
    ///     Путь к директории
    /// </summary>
    public string Path { get; set; } = "data";

    /// <summary>
    ///     Префикс пути (например, идентификатор арендатора)
    /// </summary>
    public string? Prefix { get; set; }
}