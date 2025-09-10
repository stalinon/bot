using System.Diagnostics;

using FluentAssertions;

using Stalinon.Bot.Observability;

using Xunit;

namespace Stalinon.Bot.Observability.Tests;

/// <summary>
///     Тесты Telemetry: проверка создания и завершения активности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет, что активность создаётся и завершается корректно.</item>
///     </list>
/// </remarks>
public sealed class TelemetryTests
{
    /// <inheritdoc/>
    public TelemetryTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать и завершать Activity.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать и завершать Activity")]
    public void Should_CreateAndDisposeActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        Activity? activity;
        using (activity = Telemetry.ActivitySource.StartActivity("Test"))
        {
            activity.Should().NotBeNull();
            Activity.Current.Should().Be(activity);
        }

        // Assert
        Activity.Current.Should().BeNull();
    }
}
