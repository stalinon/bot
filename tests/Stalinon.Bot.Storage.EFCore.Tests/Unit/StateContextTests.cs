using System.Linq;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Stalinon.Bot.Storage.EFCore;

using Xunit;

namespace Stalinon.Bot.Storage.EFCore.Tests;

/// <summary>
///     Тесты конфигурации StateContext
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется составной первичный ключ и индекс</item>
///         <item>Проверяется токен конкуренции для Version</item>
///     </list>
/// </remarks>
public sealed class StateContextTests
{
    /// <inheritdoc/>
    public StateContextTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен задавать составной первичный ключ и индекс на TtlUtc
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен задавать составной первичный ключ и индекс на TtlUtc")]
    public void Should_ConfigureKeyAndIndex()
    {
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new StateContext(options);

        var entity = ctx.Model.FindEntityType(typeof(StateEntry))!;
        var key = entity.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray();
        var hasTtlIndex = entity.GetIndexes().Any(i => i.Properties.Single().Name == nameof(StateEntry.TtlUtc));

        key.Should().BeEquivalentTo(new[] { nameof(StateEntry.Scope), nameof(StateEntry.Key) });
        hasTtlIndex.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 2: Должен помечать Version как токен конкуренции
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен помечать Version как токен конкуренции")]
    public void Should_MarkVersion_AsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<StateContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new StateContext(options);

        var entity = ctx.Model.FindEntityType(typeof(StateEntry))!;
        entity.FindProperty(nameof(StateEntry.Version))!.IsConcurrencyToken.Should().BeTrue();
    }
}
