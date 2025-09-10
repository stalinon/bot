using System.Text.Json;

using FluentAssertions;

using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests.Stats;

/// <summary>
///     Тесты сериализации <see cref="Snapshot" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется корректность сериализации и десериализации.</item>
///     </list>
/// </remarks>
public sealed class SnapshotSerializationTests
{
    /// <inheritdoc/>
    public SnapshotSerializationTests()
    {
    }

    /// <summary>
    ///     Тест 1: Сериализация и десериализация возвращают идентичный снимок.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Сериализация и десериализация возвращают идентичный снимок")]
    public void Should_SerializeAndDeserialize_When_UsingSystemTextJson()
    {
        var original = new Snapshot(
            new Dictionary<string, HandlerStat> { ["h"] = new(1, 2, 3, 4, 0.5) },
            1,
            2,
            3,
            4,
            0.25,
            5,
            6,
            7,
            8);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Snapshot>(json);

        restored.Should().BeEquivalentTo(original);
    }
}

