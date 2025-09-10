using System.Text.Json;

using Stalinon.Bot.Storage.File.Options;

using Xunit;

namespace Stalinon.Bot.Storage.File.Tests;

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
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions { Path = dir, BufferHotKeys = false };
        await using var store = new FileStateStore(options);
        await store.SetAsync("scope", "key", 1, null, CancellationToken.None);
        var file = Path.Combine(dir, "scope", "key.json");
        Assert.True(System.IO.File.Exists(file));
    }

    /// <summary>
    ///     Тест 2. Проверяем пакетный сброс записей
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем пакетный сброс записей")]
    public async Task WriteWithBuffer()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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

        var file = Path.Combine(dir, "scope", "key.json");
        Assert.False(System.IO.File.Exists(file));
        await Task.Delay(options.FlushPeriod + TimeSpan.FromMilliseconds(200));
        var content = await System.IO.File.ReadAllTextAsync(file);
        var value = JsonSerializer.Deserialize<int>(content);
        Assert.Equal(999, value);
    }

    /// <summary>
    ///     Тест 3. Проверяем префикс и нормализацию
    /// </summary>
    [Fact(DisplayName = "Тест 3. Проверяем префикс и нормализацию")]
    public async Task PrefixAndNormalization()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new FileStateStore(new FileStoreOptions { Path = temp, Prefix = "tenant" });
        var ct = CancellationToken.None;
        await store.SetAsync("user", "ping:42", 1, null, ct);
        var expected = Path.Combine(temp, "tenant", "user", "ping", "42.json");
        Assert.True(System.IO.File.Exists(expected));
        var value = await store.GetAsync<int>("user", "ping:42", ct);
        Assert.Equal(1, value);
    }

    /// <summary>
    ///     Тест 4. Удаление по TTL
    /// </summary>
    [Fact(DisplayName = "Тест 4. Удаление по TTL")]
    public async Task Remove_after_ttl()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions { Path = path, CleanUpPeriod = TimeSpan.FromMilliseconds(50) };
        await using var store = new FileStateStore(options);

        await store.SetAsync<int?>("s", "k", 1, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        await Task.Delay(200);

        Assert.Null(await store.GetAsync<int?>("s", "k", CancellationToken.None));
        var file = Path.Combine(path, "s", "k.json");
        Assert.False(System.IO.File.Exists(file));

        Directory.Delete(path, true);
    }
}
