using FluentAssertions;

using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests.Stats;

/// <summary>
///     Тесты применения пользовательских метрик <see cref="CustomStats" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется накопление счётчиков и гистограмм.</item>
///         <item>Проверяется сброс метрик в новом экземпляре.</item>
///     </list>
/// </remarks>
public sealed class CustomStatsUsageTests
{
    /// <inheritdoc/>
    public CustomStatsUsageTests()
    {
    }

    /// <summary>
    ///     Тест 1: Счётчики и гистограммы накапливаются и сбрасываются в новом экземпляре.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Счётчики и гистограммы накапливаются и сбрасываются в новом экземпляре")]
    public void Should_AccumulateAndReset_When_UsingCustomStats()
    {
        var stats = new CustomStats();
        stats.Increment("counter");
        stats.Increment("counter", 2);
        stats.Record("hist", 1);
        stats.Record("hist", 3);

        var snapshot = stats.GetSnapshot();
        snapshot.Counters.Should().ContainKey("counter").WhoseValue.Should().Be(3);
        snapshot.Histograms.Should().ContainKey("hist");

        stats = new CustomStats();
        var empty = stats.GetSnapshot();
        empty.Counters.Should().BeEmpty();
        empty.Histograms.Should().BeEmpty();
    }
}

