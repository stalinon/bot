using System.Diagnostics;

using Bot.Storage.File;
using Bot.Storage.File.Options;

using FluentAssertions;

using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Нагрузочные тесты хранилищ.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Сравнивается производительность Redis и файла</item>
///         <item>Оценивается время инкремента</item>
///     </list>
/// </remarks>
public sealed class StoragePerformanceTests : IClassFixture<RedisFixture>, IAsyncLifetime
{
    private readonly RedisStateStore _redis;
    private readonly FileStateStore _file;

    /// <inheritdoc/>
    public StoragePerformanceTests(RedisFixture fixture)
    {
        var options = new RedisOptions { Connection = fixture.Connection };
        _redis = new RedisStateStore(options);
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _file = new FileStateStore(new FileStoreOptions { Path = dir });
    }

    /// <summary>
    ///     Тест 1: Redis не хуже File на инкременте.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Redis не хуже File на инкременте", Skip = "Требуется стабильный Redis")]
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

        swRedis.Elapsed.Should().BeLessOrEqualTo(swFile.Elapsed * 2, $"Redis slower: file {swFile.Elapsed}, redis {swRedis.Elapsed}");
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
