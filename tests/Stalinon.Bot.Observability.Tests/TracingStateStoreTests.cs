using System.Diagnostics;

using FluentAssertions;

using Moq;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Observability;

using Xunit;

namespace Stalinon.Bot.Observability.Tests;

/// <summary>
///     Тесты TracingStateStore: проверка передачи активности и вызовов внутреннего хранилища.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет чтение с созданием активности.</item>
///         <item>Проверяет запись с созданием активности.</item>
///     </list>
/// </remarks>
public sealed class TracingStateStoreTests
{
    /// <inheritdoc/>
    public TracingStateStoreTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать Activity и вызывать внутреннее хранилище при чтении.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать Activity и вызывать внутреннее хранилище при чтении")]
    public async Task Should_CreateActivityAndCallInner_When_GetAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<IStateStore>();
        Activity? captured = null;
        inner.Setup(x => x.GetAsync<string>("scope", "key", It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .ReturnsAsync("value");
        var sut = new TracingStateStore(inner.Object);

        // Act
        var result = await sut.GetAsync<string>("scope", "key", CancellationToken.None);

        // Assert
        result.Should().Be("value");
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Store/Get");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.GetAsync<string>("scope", "key", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 2: Должен создавать Activity и вызывать внутреннее хранилище при записи.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен создавать Activity и вызывать внутреннее хранилище при записи")]
    public async Task Should_CreateActivityAndCallInner_When_SetAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<IStateStore>();
        Activity? captured = null;
        inner.Setup(x => x.SetAsync("scope", "key", "value", TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingStateStore(inner.Object);

        // Act
        await sut.SetAsync("scope", "key", "value", TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Store/Set");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.SetAsync("scope", "key", "value", TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()), Times.Once);
    }
}
