using System.Text.Json;

using Bot.Storage.File.Options;

using Xunit;

namespace Bot.Storage.File.Tests;

/// <summary>
///     Тесты файлового хранилища
/// </summary>
public sealed class FileStateStoreTests
{
    /// <summary>
    ///     Тест 1. Проверяем мгновенную запись без буфера
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем мгновенную запись без буфера")]
    public async Task WriteWithoutBuffer()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions { Path = dir, BufferHotKeys = false };
        await using var store = new FileStateStore(options);
        await store.SetAsync("scope", "key", 1, null, CancellationToken.None);
        var file = System.IO.Path.Combine(dir, "scope", "key.json");
        Assert.True(System.IO.File.Exists(file));
    }

    /// <summary>
    ///     Тест 2. Проверяем пакетный сброс записей
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем пакетный сброс записей")]
    public async Task WriteWithBuffer()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions
        {
            Path = dir,
            BufferHotKeys = true,
            FlushPeriod = TimeSpan.FromMilliseconds(500)
        };
        await using var store = new FileStateStore(options);
        for (var i = 0; i < 1000; i++)
        {
            await store.SetAsync("scope", "key", i, null, CancellationToken.None);
        }
        var file = System.IO.Path.Combine(dir, "scope", "key.json");
        Assert.False(System.IO.File.Exists(file));
        await Task.Delay(options.FlushPeriod + TimeSpan.FromMilliseconds(200));
        var content = await System.IO.File.ReadAllTextAsync(file);
        var value = JsonSerializer.Deserialize<int>(content);
        Assert.Equal(999, value);
    }
}
