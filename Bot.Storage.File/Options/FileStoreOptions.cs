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
    ///     Включить буферизацию горячих ключей
    /// </summary>
    public bool BufferHotKeys { get; set; }

    /// <summary>
    ///     Период сброса буфера
    /// </summary>
    public TimeSpan FlushPeriod { get; set; } = TimeSpan.FromSeconds(1);
}
