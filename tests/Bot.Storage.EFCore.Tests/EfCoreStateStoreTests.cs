using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Bot.Storage.EFCore.Tests;

/// <summary>
///     Тесты EF Core хранилища состояний
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется сохранение и чтение значения</item>
///         <item>Проверяется применение миграций при инициализации</item>
///         <item>Проверяется успешный compare-and-swap</item>
///         <item>Проверяется отказ compare-and-swap при несоответствии</item>
///     </list>
/// </remarks>
public sealed class EfCoreStateStoreTests
{
    /// <inheritdoc />
    public EfCoreStateStoreTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен сохранять и возвращать значение
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен сохранять и возвращать значение")]
    public async Task Should_SaveAndReturnValue()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);

        await store.SetAsync("s", "k", 42, null, CancellationToken.None);
        var value = await store.GetAsync<int>("s", "k", CancellationToken.None);

        value.Should().Be(42);
    }

    /// <summary>
    ///     Тест 2: Должен применять миграции при инициализации
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен применять миграции при инициализации")]
    public async Task Should_RunMigrations_OnInitialization()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);

        var migrations = await ctx.Database.GetAppliedMigrationsAsync();

        migrations.Should().Contain("20240517000000_Initial");
    }

    /// <summary>
    ///     Тест 3: Должен заменить значение при совпадении текущего с ожидаемым
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен заменить значение при совпадении текущего с ожидаемым")]
    public async Task Should_UpdateValue_WhenCurrentEqualsExpected()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);

        await store.SetAsync("s", "k", 1, null, CancellationToken.None);

        var result = await store.TrySetIfAsync("s", "k", 1, 2, null, CancellationToken.None);
        var value = await store.GetAsync<int>("s", "k", CancellationToken.None);

        result.Should().BeTrue();
        value.Should().Be(2);
    }

    /// <summary>
    ///     Тест 4: Должен вернуть false и не менять значение при несовпадении текущего с ожидаемым
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен вернуть false и не менять значение при несовпадении текущего с ожидаемым")]
    public async Task Should_NotUpdate_WhenCurrentDiffersFromExpected()
    {
        var file = Path.GetTempFileName();
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseSqlite($"Data Source={file}", b => b.MigrationsAssembly(typeof(StateContext).Assembly.FullName))
            .Options;
        await using var ctx = new StateContext(options);
        var store = new EfCoreStateStore(ctx);

        await store.SetAsync("s", "k", 1, null, CancellationToken.None);

        var result = await store.TrySetIfAsync("s", "k", 0, 2, null, CancellationToken.None);
        var value = await store.GetAsync<int>("s", "k", CancellationToken.None);

        result.Should().BeFalse();
        value.Should().Be(1);
    }
}
