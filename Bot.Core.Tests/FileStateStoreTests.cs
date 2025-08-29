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
}
