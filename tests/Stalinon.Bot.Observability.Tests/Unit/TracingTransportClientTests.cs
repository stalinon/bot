using System.Diagnostics;
using System.IO;

using FluentAssertions;

using Moq;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Observability;

using Telegram.Bot;

using Xunit;

namespace Stalinon.Bot.Observability.Tests;

/// <summary>
///     Тесты TracingTransportClient: проверка передачи активности и вызовов внутреннего клиента.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет отправку текста.</item>
///         <item>Проверяет отправку фото.</item>
///         <item>Проверяет редактирование текста сообщения.</item>
///         <item>Проверяет редактирование подписи сообщения.</item>
///         <item>Проверяет отправку действия в чат.</item>
///         <item>Проверяет удаление сообщения.</item>
///         <item>Проверяет вызов нативного клиента.</item>
///     </list>
/// </remarks>
public sealed class TracingTransportClientTests
{
    /// <inheritdoc/>
    public TracingTransportClientTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать Activity и вызывать внутренний клиент при отправке текста.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать Activity и вызывать внутренний клиент при отправке текста")]
    public async Task Should_CreateActivityAndCallInner_When_SendTextAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        inner.Setup(x => x.SendTextAsync(chat, "t", It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.SendTextAsync(chat, "t", CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.SendTextAsync(chat, "t", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 2: Должен создавать Activity и вызывать внутренний клиент при отправке фото.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен создавать Activity и вызывать внутренний клиент при отправке фото")]
    public async Task Should_CreateActivityAndCallInner_When_SendPhotoAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        var stream = Stream.Null;
        inner.Setup(x => x.SendPhotoAsync(chat, stream, null, It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.SendPhotoAsync(chat, stream, null, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.SendPhotoAsync(chat, stream, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 3: Должен создавать Activity и вызывать внутренний клиент при редактировании текста.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен создавать Activity и вызывать внутренний клиент при редактировании текста")]
    public async Task Should_CreateActivityAndCallInner_When_EditMessageTextAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        inner.Setup(x => x.EditMessageTextAsync(chat, 1, "t", It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.EditMessageTextAsync(chat, 1, "t", CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.EditMessageTextAsync(chat, 1, "t", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 4: Должен создавать Activity и вызывать внутренний клиент при редактировании подписи.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен создавать Activity и вызывать внутренний клиент при редактировании подписи")]
    public async Task Should_CreateActivityAndCallInner_When_EditMessageCaptionAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        inner.Setup(x => x.EditMessageCaptionAsync(chat, 1, null, It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.EditMessageCaptionAsync(chat, 1, null, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.EditMessageCaptionAsync(chat, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 5: Должен создавать Activity и вызывать внутренний клиент при отправке действия.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен создавать Activity и вызывать внутренний клиент при отправке действия")]
    public async Task Should_CreateActivityAndCallInner_When_SendChatActionAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        inner.Setup(x => x.SendChatActionAsync(chat, ChatAction.Typing, It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.SendChatActionAsync(chat, ChatAction.Typing, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.SendChatActionAsync(chat, ChatAction.Typing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 6: Должен создавать Activity и вызывать внутренний клиент при удалении сообщения.
    /// </summary>
    [Fact(DisplayName = "Тест 6: Должен создавать Activity и вызывать внутренний клиент при удалении сообщения")]
    public async Task Should_CreateActivityAndCallInner_When_DeleteMessageAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        var chat = new ChatAddress(1);
        inner.Setup(x => x.DeleteMessageAsync(chat, 1, It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.DeleteMessageAsync(chat, 1, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.DeleteMessageAsync(chat, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 7: Должен создавать Activity и вызывать внутренний клиент при вызове нативного клиента.
    /// </summary>
    [Fact(DisplayName = "Тест 7: Должен создавать Activity и вызывать внутренний клиент при вызове нативного клиента.")]
    public async Task Should_CreateActivityAndCallInner_When_CallNativeClientAsync()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var inner = new Mock<ITransportClient>();
        Activity? captured = null;
        inner.Setup(x => x.CallNativeClientAsync(It.IsAny<Func<ITelegramBotClient, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Callback(() => captured = Activity.Current)
            .Returns(Task.CompletedTask);
        var sut = new TracingTransportClient(inner.Object);

        // Act
        await sut.CallNativeClientAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("Transport/Send");
        Activity.Current.Should().BeNull();
        inner.Verify(x => x.CallNativeClientAsync(It.IsAny<Func<ITelegramBotClient, CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
