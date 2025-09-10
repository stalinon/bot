using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Admin.MinimalApi;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты пробы транспорта.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Вызывает транспортный клиент.</item>
///         <item>Передаёт исключение дальше.</item>
///     </list>
/// </remarks>
public sealed class TransportProbeTests
{
    /// <summary>
    ///     Тест 1: Проба транспорта вызывает клиент один раз
    /// </summary>
    [Fact(DisplayName = "Тест 1: Проба транспорта вызывает клиент один раз")]
    public async Task Should_CallClient_When_ProbeAsync()
    {
        var client = new Mock<ITransportClient>();
        client.Setup(x => x.SendChatActionAsync(It.IsAny<ChatAddress>(), It.IsAny<ChatAction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var probe = new TransportProbe(client.Object);

        await probe.ProbeAsync(CancellationToken.None);

        client.Verify(x => x.SendChatActionAsync(It.IsAny<ChatAddress>(), It.IsAny<ChatAction>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Тест 2: Проба транспорта бросает исключение при ошибке клиента
    /// </summary>
    [Fact(DisplayName = "Тест 2: Проба транспорта бросает исключение при ошибке клиента")]
    public async Task Should_Throw_When_ClientFails()
    {
        var client = new Mock<ITransportClient>();
        client.Setup(x => x.SendChatActionAsync(It.IsAny<ChatAddress>(), It.IsAny<ChatAction>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));
        var probe = new TransportProbe(client.Object);

        var act = () => probe.ProbeAsync(CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }
}

