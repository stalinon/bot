using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bot.Storage.File;
using Bot.Storage.File.Options;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты файлового хранилища
/// </summary>
public sealed class FileStateStoreTests
{
    /// <summary>
    ///     Проверяем префикс и нормализацию пути
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем префикс и нормализацию")]
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
    ///     Проверяет удаление записи после истечения срока жизни.
    /// </summary>
    [Fact(DisplayName = "Тест 2. Удаление по TTL")]
    public async Task Remove_after_ttl()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions { Path = path, CleanUpPeriod = TimeSpan.FromMilliseconds(50) };
        await using var store = new FileStateStore(options);

        await store.SetAsync<int?>("s", "k", 1, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        await Task.Delay(200);

        Assert.Null(await store.GetAsync<int?>("s", "k", CancellationToken.None));
        var file = Path.Combine(path, "s", "k.json");
        Assert.False(File.Exists(file));

        Directory.Delete(path, true);
    }
}
