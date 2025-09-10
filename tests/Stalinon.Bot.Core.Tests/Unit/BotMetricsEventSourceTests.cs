using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

using FluentAssertions;

using Stalinon.Bot.Core.Metrics;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="BotMetricsEventSource" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются стандартные счётчики.</item>
///         <item>Проверяются пользовательские метрики.</item>
///         <item>Проверяется освобождение ресурсов.</item>
///     </list>
/// </remarks>
public sealed class BotMetricsEventSourceTests
{
    /// <inheritdoc/>
    public BotMetricsEventSourceTests()
    {
    }

    /// <summary>
    ///     Тест 1: Стандартные счётчики и задержки обновляются.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Стандартные счётчики и задержки обновляются")]
    public async Task Should_UpdateStandardCounters_When_MethodsCalled()
    {
        // Arrange
        using var listener = new BotEventListener();
        var es = BotMetricsEventSource.Log;

        // Act
        es.Update(30, true);
        es.Update(40, false);
        es.Handler(10, false);
        es.MarkDroppedUpdate();
        es.MarkRateLimited();
        es.MarkLostUpdates(3);
        es.SetQueueDepth(5);
        await Task.Delay(1100);

        // Assert
        listener.Values["tgbot_updates_total"].Should().Be(2);
        listener.Values["tgbot_errors_total"].Should().Be(2);
        listener.Values["tgbot_dropped_updates_total"].Should().Be(1);
        listener.Values["tgbot_rate_limited_total"].Should().Be(1);
        listener.Values["tgbot_lost_updates_total"].Should().Be(3);
        listener.Values["tgbot_queue_depth"].Should().Be(5);
        listener.Values["tgbot_update_latency_ms"].Should().BeGreaterThan(0);
        listener.Values["tgbot_handler_latency_ms"].Should().Be(10);
    }

    /// <summary>
    ///     Тест 2: Пользовательские счётчик и гистограмма экспортируются.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пользовательские счётчик и гистограмма экспортируются")]
    public async Task Should_ExportCustomMetrics_When_Recorded()
    {
        // Arrange
        using var listener = new BotEventListener();
        var es = BotMetricsEventSource.Log;

        // Act
        es.CustomCounter("my_counter", 2);
        es.CustomHistogram("my_hist", 1);
        es.CustomHistogram("my_hist", 3);
        await Task.Delay(1100);

        // Assert
        listener.Values["my_counter"].Should().Be(2);
        listener.Values["my_hist"].Should().Be(2);
    }

    /// <summary>
    ///     Тест 3: Dispose можно вызывать повторно без ошибок.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Dispose можно вызывать повторно без ошибок")]
    public void Should_AllowMultipleDisposeCalls()
    {
        // Arrange
        var es = (BotMetricsEventSource)Activator.CreateInstance(typeof(BotMetricsEventSource), true)!;

        // Act
        var act = () =>
        {
            es.Dispose();
            es.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    private sealed class BotEventListener : EventListener
    {
        public ConcurrentDictionary<string, double> Values { get; } = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Stalinon.Bot.Core")
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string?>
                {
                    ["EventCounterIntervalSec"] = "1"
                });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "EventCounters" || eventData.Payload is null || eventData.Payload.Count == 0)
            {
                return;
            }

            if (eventData.Payload[0] is not IDictionary<string, object> payload)
            {
                return;
            }

            var name = payload["Name"].ToString()!;
            if (payload.TryGetValue("Increment", out var inc))
            {
                Values[name] = Convert.ToDouble(inc);
            }
            else if (payload.TryGetValue("Mean", out var mean))
            {
                Values[name] = Convert.ToDouble(mean);
            }
        }
    }
}
