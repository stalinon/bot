namespace Stalinon.Bot.Storage.File.Options;

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
    ///     Включить буферизацию горячих ключей
    /// </summary>
    public bool BufferHotKeys { get; set; }

    /// <summary>
    ///     Период сброса буфера
    /// </summary>
    public TimeSpan FlushPeriod { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Префикс пути (например, идентификатор арендатора)
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    ///     Период очистки просроченных ключей
    /// </summary>
    public TimeSpan CleanUpPeriod { get; set; } = TimeSpan.FromMinutes(5);
}
