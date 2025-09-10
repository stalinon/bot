using System;
using System.Threading.Tasks;

using FluentAssertions;

using Stalinon.Bot.Core.Utils;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты TtlCache: проверка добавления, чтения и истечения ключей.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется добавление нового ключа.</item>
///         <item>Проверяется запрет повторного добавления до истечения.</item>
///         <item>Проверяется истечение ключа.</item>
///         <item>Проверяется повторное добавление после истечения.</item>
///     </list>
/// </remarks>
public sealed class TtlCacheTests
{
    /// <inheritdoc/>
    public TtlCacheTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен добавлять новый ключ.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен добавлять новый ключ")]
    public void Should_AddNewKey()
    {
        var cache = new TtlCache<string>(TimeSpan.FromSeconds(1));

        var added = cache.TryAdd("a");

        added.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 2: Должен возвращать false при повторном добавлении до истечения TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен возвращать false при повторном добавлении до истечения TTL")]
    public void Should_ReturnFalse_When_KeyExists()
    {
        var cache = new TtlCache<string>(TimeSpan.FromSeconds(1));
        cache.TryAdd("a");

        var added = cache.TryAdd("a");

        added.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 3: Должен позволять повторное добавление после истечения TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен позволять повторное добавление после истечения TTL")]
    public async Task Should_AddAgain_After_Expiration()
    {
        var cache = new TtlCache<string>(TimeSpan.FromMilliseconds(100));
        cache.TryAdd("a");

        await Task.Delay(200);

        var added = cache.TryAdd("a");

        added.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 4: Должен запрещать добавление до повторного истечения TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен запрещать добавление до повторного истечения TTL")]
    public async Task Should_NotAdd_Before_Reexpiration()
    {
        var cache = new TtlCache<string>(TimeSpan.FromMilliseconds(100));
        cache.TryAdd("a");
        await Task.Delay(200);
        cache.TryAdd("a");

        var added = cache.TryAdd("a");

        added.Should().BeFalse();
    }
}
