using System.Net;
using System.Reflection;
using System.Threading.Channels;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Hosting.Tests;

/// <summary>
///     Тесты проб готовности.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется успешный ответ при пустой очереди.</item>
///         <item>Проверяется код 503 при переполненной очереди.</item>
///     </list>
/// </remarks>
public sealed class HealthEndpointTests : IClassFixture<HostingFactory>
{
    private readonly HostingFactory _factory;

    /// <inheritdoc />
    public HealthEndpointTests(HostingFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Возвращает 200 при пустой очереди.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Возвращает 200 при пустой очереди")]
    public async Task Should_ReturnOk_WhenQueueEmpty()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Тест 2: Возвращает 503 при переполненной очереди.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Возвращает 503 при переполненной очереди")]
    public async Task Should_ReturnServiceUnavailable_WhenQueueIsFull()
    {
        var hosted = _factory.Services.GetRequiredService<BotHostedService>();
        var channelField =
            typeof(BotHostedService).GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var channel = (Channel<UpdateContext>)channelField.GetValue(hosted)!;
        for (var i = 0; i < 16; i++)
        {
            var ctx = new UpdateContext(
                "test",
                i.ToString(),
                new ChatAddress(i),
                new UserAddress(i),
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                null!,
                CancellationToken.None);
            channel.Writer.TryWrite(ctx);
        }

        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
