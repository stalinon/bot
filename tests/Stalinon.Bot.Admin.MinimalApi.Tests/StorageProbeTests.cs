using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Admin.MinimalApi;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты пробы хранилища.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Выполняет запись и чтение.</item>
///         <item>Передаёт исключения.</item>
///     </list>
/// </remarks>
public sealed class StorageProbeTests
{
    /// <summary>
    ///     Тест 1: Проба хранилища выполняет запись и чтение
    /// </summary>
    [Fact(DisplayName = "Тест 1: Проба хранилища выполняет запись и чтение")]
    public async Task Should_CallSetAndGet_When_ProbeAsync()
    {
        var store = new Mock<IStateStore>();
        store.Setup(x => x.SetAsync("health", "probe", 0, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(x => x.GetAsync<int>("health", "probe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var probe = new StorageProbe(store.Object);

        await probe.ProbeAsync(CancellationToken.None);

        store.Verify(x => x.SetAsync("health", "probe", 0, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.GetAsync<int>("health", "probe", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Тест 2: Проба хранилища бросает исключение при ошибке записи
    /// </summary>
    [Fact(DisplayName = "Тест 2: Проба хранилища бросает исключение при ошибке записи")]
    public async Task Should_Throw_When_SetFails()
    {
        var store = new Mock<IStateStore>();
        store.Setup(x => x.SetAsync("health", "probe", 0, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));
        var probe = new StorageProbe(store.Object);

        var act = () => probe.ProbeAsync(CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }
}

