using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты пробы очереди.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Возвращает completed при глубине ниже порога.</item>
///         <item>Бросает исключение при превышении порога.</item>
///     </list>
/// </remarks>
public sealed class QueueProbeTests
{
    /// <summary>
    ///     Тест 1: Проба очереди завершается без ошибок при глубине ниже порога
    /// </summary>
    [Fact(DisplayName = "Тест 1: Проба очереди завершается без ошибок при глубине ниже порога")]
    public async Task Should_NotThrow_When_DepthBelowThreshold()
    {
        var stats = new StatsCollector();
        stats.SetQueueDepth(100);
        var probe = new QueueProbe(stats);

        var act = () => probe.ProbeAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    ///     Тест 2: Проба очереди бросает исключение при глубине выше порога
    /// </summary>
    [Fact(DisplayName = "Тест 2: Проба очереди бросает исключение при глубине выше порога")]
    public async Task Should_Throw_When_DepthAboveThreshold()
    {
        var stats = new StatsCollector();
        stats.SetQueueDepth(2000);
        var probe = new QueueProbe(stats);

        var act = () => probe.ProbeAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

