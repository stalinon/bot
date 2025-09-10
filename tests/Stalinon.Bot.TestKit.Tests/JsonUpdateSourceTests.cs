using System.Text.Json;

using FluentAssertions;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты JsonUpdateSource: корректный парсинг и обработка ошибок.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется чтение валидного JSON.</item>
///         <item>Проверяется исключение при неверном формате.</item>
///     </list>
/// </remarks>
public sealed class JsonUpdateSourceTests
{
    private static readonly string[] Args = new[] { "a", "b" };
    /// <inheritdoc />
    public JsonUpdateSourceTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен читать обновления из JSON.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен читать обновления из JSON")]
    public async Task Should_ReadUpdates_FromJson()
    {
        var path = Path.GetTempFileName();
        var json = """
        [
            {
                "transport": "tg",
                "updateId": "1",
                "chat": { "id": 1 },
                "user": { "id": 2 },
                "text": "hi",
                "command": "start",
                "args": ["a", "b"],
                "payload": "p",
                "items": { "k": 1 }
            },
            {
                "text": "bye"
            }
        ]
        """;
        await File.WriteAllTextAsync(path, json);
        var source = new JsonUpdateSource(path);
        var received = new List<UpdateContext>();

        await source.StartAsync(ctx =>
        {
            received.Add(ctx);
            return Task.CompletedTask;
        }, CancellationToken.None);

        received.Should().HaveCount(2);
        var first = received[0];
        first.Transport.Should().Be("tg");
        first.UpdateId.Should().Be("1");
        first.Chat.Id.Should().Be(1);
        first.User.Id.Should().Be(2);
        first.Text.Should().Be("hi");
        first.Command.Should().Be("start");
        first.Args.Should().BeEquivalentTo(Args);
        first.Payload.Should().Be("p");
        first.Items.Should().ContainKey("k");

        var second = received[1];
        second.Transport.Should().Be("test");
        second.UpdateId.Should().BeEmpty();
        second.Chat.Id.Should().Be(0);
        second.User.Id.Should().Be(0);
        second.Text.Should().Be("bye");
        second.Command.Should().BeNull();
        second.Args.Should().BeNull();
        second.Payload.Should().BeNull();
        second.Items.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 2: Должен выбрасывать исключение при неверном JSON.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выбрасывать исключение при неверном JSON")]
    public async Task Should_Throw_OnInvalidJson()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "oops");
        var source = new JsonUpdateSource(path);
        var called = false;

        var act = async () => await source.StartAsync(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await act.Should().ThrowAsync<JsonException>();
        called.Should().BeFalse();
    }
}

