using Bot.Abstractions.Contracts;
using Bot.Core.Options;
using Bot.Core.Transport;
using Bot.Hosting.Options;
using Bot.Outbox;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Telegram.Tests;

/// <summary>
///     Тесты регистрации сервисов Telegram.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется оборачивание транспорта в аутбокс.</item>
///     </list>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    /// <inheritdoc />
    public ServiceCollectionExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует транспорт с аутбоксом.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует транспорт с аутбоксом.")]
    public void Should_RegisterOutboxTransport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<BotOptions>().Configure(o => o.Token = "1:token");
        services.AddSingleton<IOutbox, DummyOutbox>();

        // Act
        services.AddTelegramTransport();
        var sp = services.BuildServiceProvider();

        // Assert
        var client = sp.GetRequiredService<ITransportClient>();
        client.Should().BeOfType<OutboxTransportClient>();
    }

    private sealed class DummyOutbox : IOutbox
    {
        public Task<long> GetPendingAsync(CancellationToken ct) => Task.FromResult(0L);

        public Task SendAsync(string id, string payload, Func<string, string, CancellationToken, Task> transport, CancellationToken ct)
        {
            return transport(id, payload, ct);
        }
    }
}
