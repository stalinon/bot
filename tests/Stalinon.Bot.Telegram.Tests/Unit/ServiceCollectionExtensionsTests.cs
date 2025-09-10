using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Transport;
using Stalinon.Bot.Hosting.Options;
using Stalinon.Bot.Observability;
using Stalinon.Bot.Outbox;

using Xunit;

namespace Stalinon.Bot.Telegram.Tests;

/// <summary>
///     Тесты регистрации сервисов Telegram.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется подключение транспорта с трассировкой.</item>
///     </list>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    /// <inheritdoc />
    public ServiceCollectionExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует транспорт с трассировкой.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует транспорт с трассировкой.")]
    public void Should_RegisterTracingTransport()
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
        client.Should().BeOfType<TracingTransportClient>();
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
