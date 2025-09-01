using System.IO;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bot.Storage.EFCore.Tests;

/// <summary>
///     Тесты EF Core хранилища
/// </summary>
public sealed class EfCoreStateStoreTests
{
    /// <summary>
    ///     Тест 1. Проверяем чтение и запись
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем чтение и запись")]
    public async Task WriteRead()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);
        await store.SetAsync("s", "k", 42, null, CancellationToken.None);
        var v = await store.GetAsync<int>("s", "k", CancellationToken.None);
        Assert.Equal(42, v);
    }

    /// <summary>
    ///     Тест 2. Проверяем применение миграций
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем применение миграций")]
    public async Task MigrationsRun()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);
        var migrations = await ctx.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20240517000000_Initial", migrations);
    }
}
