using System.Diagnostics;

using Bot.Storage.File;
using Bot.Storage.File.Options;

using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Нагрузочные тесты хранилищ.
/// </summary>
public sealed class StoragePerformanceTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisStateStore _redis;
    private readonly FileStateStore _file;

    /// <summary>
    ///     Создаёт тесты.
    /// </summary>
    /// <param name="fixture">Фикстура Redis.</param>
    public StoragePerformanceTests(RedisFixture fixture)
    {
        _redis = new RedisStateStore(fixture.Connection);
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _file = new FileStateStore(new FileStoreOptions { Path = dir });
    }

    /// <summary>
    ///     Тест 1. Redis не хуже File на инкременте.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Redis не хуже File на инкременте", Skip = "Требуется стабильный Redis")]
    public async Task RedisNotSlowerThanFileOnIncrement()
    {
        const int n = 100;
        var swFile = Stopwatch.StartNew();
        for (var i = 0; i < n; i++)
        {
            await _file.IncrementAsync("s", "k", 1, null, CancellationToken.None);
        }
        swFile.Stop();

        var swRedis = Stopwatch.StartNew();
        for (var i = 0; i < n; i++)
        {
            await _redis.IncrementAsync("s", "k", 1, null, CancellationToken.None);
        }
        swRedis.Stop();

        Assert.True(swRedis.Elapsed <= swFile.Elapsed * 2, $"Redis slower: file {swFile.Elapsed}, redis {swRedis.Elapsed}");
    }

    /// <summary>
    ///     Инициализация перед запуском тестов.
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    ///     Очистка после выполнения тестов.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _file.DisposeAsync();
    }
}
