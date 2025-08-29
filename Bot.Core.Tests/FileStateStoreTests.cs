using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bot.Storage.File;
using Bot.Storage.File.Options;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты файлового хранилища.
/// </summary>
public class FileStateStoreTests
{
    /// <summary>
    ///     Проверяет удаление записи после истечения срока жизни.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Удаление по TTL")]
    public async Task Remove_after_ttl()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions { Path = path, CleanUpPeriod = TimeSpan.FromMilliseconds(50) };
        using var store = new FileStateStore(options);

        await store.SetAsync<int?>("s", "k", 1, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        await Task.Delay(200);

        Assert.Null(await store.GetAsync<int?>("s", "k", CancellationToken.None));
        var file = Path.Combine(path, "s", "k.json");
        Assert.False(File.Exists(file));

        Directory.Delete(path, true);
    }
}
