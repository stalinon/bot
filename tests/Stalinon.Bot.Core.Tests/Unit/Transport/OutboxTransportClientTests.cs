using System;

using FluentAssertions;

using Moq;

using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Transport;
using Stalinon.Bot.Outbox;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты клиента отправки через аутбокс.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется вызов аутбокса и транспорта при отправке текста.</item>
///         <item>Проверяется отсутствие вызова транспорта при ошибке аутбокса.</item>
///     </list>
/// </remarks>
public sealed class OutboxTransportClientTests
{
    /// <inheritdoc />
    public OutboxTransportClientTests()
    {
    }

    /// <summary>
    ///     Тест 1: Отправляет текст через аутбокс.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Отправляет текст через аутбокс.")]
    public async Task Should_SendText_ThroughOutbox()
    {
        // Arrange
        var inner = new Mock<ITransportClient>();
        var outbox = new Mock<IOutbox>();
        var provider = new StubKeyProvider("k");
        outbox.Setup(x => x.SendAsync("k", It.IsAny<string>(), It.IsAny<Func<string, string, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, Func<string, string, CancellationToken, Task>, CancellationToken>((id, payload, transport, ct) => transport(id, payload, ct));
        var sut = new OutboxTransportClient(inner.Object, outbox.Object, provider);

        // Act
        await sut.SendTextAsync(new ChatAddress(1), "t", CancellationToken.None);

        // Assert
        outbox.Verify(x => x.SendAsync("k", It.IsAny<string>(), It.IsAny<Func<string, string, CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        inner.Verify(x => x.SendTextAsync(It.Is<ChatAddress>(c => c.Id == 1), "t", It.IsAny<CancellationToken>()), Times.Once);
        inner.Invocations.Should().HaveCount(1);
    }

    /// <summary>
    ///     Тест 2: Не вызывает транспорт при ошибке аутбокса.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Не вызывает транспорт при ошибке аутбокса.")]
    public async Task Should_NotCallTransport_WhenOutboxFails()
    {
        // Arrange
        var inner = new Mock<ITransportClient>();
        var outbox = new Mock<IOutbox>();
        var provider = new StubKeyProvider("k");
        outbox.Setup(x => x.SendAsync("k", It.IsAny<string>(), It.IsAny<Func<string, string, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var sut = new OutboxTransportClient(inner.Object, outbox.Object, provider);

        // Act
        var act = async () => await sut.SendTextAsync(new ChatAddress(1), "t", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        inner.Verify(x => x.SendTextAsync(It.IsAny<ChatAddress>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubKeyProvider(string key) : IMessageKeyProvider
    {
        public string Next() => key;
    }
}
