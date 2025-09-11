using System.IO;

using FluentAssertions;

using Stalinon.Bot.Abstractions.Addresses;

using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты FakeTransportClient: сохранение текстов и игнорирование остальных операций.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется запись текстовых сообщений</item>
///         <item>Проверяется игнорирование других операций</item>
///     </list>
/// </remarks>
public sealed class FakeTransportClientTests
{
    /// <inheritdoc/>
    public FakeTransportClientTests()
    {
    }

    /// <summary>
    ///     Тест 1: Клиент должен сохранять отправленные тексты.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Клиент должен сохранять отправленные тексты")]
    public async Task Should_StoreSentTexts()
    {
        // Arrange
        var client = new FakeTransportClient();
        var chat1 = new ChatAddress(1);
        var chat2 = new ChatAddress(2);

        // Act
        await client.SendTextAsync(chat1, "a", CancellationToken.None);
        await client.SendTextAsync(chat2, "b", CancellationToken.None);

        // Assert
        client.SentTexts.Should().ContainInOrder((chat1, "a"), (chat2, "b"));
    }

    /// <summary>
    ///     Тест 2: Клиент не должен сохранять другие операции.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Клиент не должен сохранять другие операции")]
    public async Task Should_IgnoreOtherOperations()
    {
        // Arrange
        var client = new FakeTransportClient();
        var chat = new ChatAddress(1);

        // Act
        await client.SendPhotoAsync(chat, Stream.Null, null, CancellationToken.None);
        await client.EditMessageTextAsync(chat, 1, "x", CancellationToken.None);
        await client.DeleteMessageAsync(chat, 1, CancellationToken.None);

        // Assert
        client.SentTexts.Should().BeEmpty();
    }
}

