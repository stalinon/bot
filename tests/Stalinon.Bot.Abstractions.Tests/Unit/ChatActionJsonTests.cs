using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using Stalinon.Bot.Abstractions;

using Xunit;

namespace Stalinon.Bot.Abstractions.Tests;

/// <summary>
///     Тесты <see cref="ChatAction"/>: сериализация и десериализация значений согласно документации Telegram.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется соответствие строковым именам Telegram</item>
///     </list>
/// </remarks>
public sealed class ChatActionJsonTests
{
    /// <inheritdoc/>
    public ChatActionJsonTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен сериализовать и десериализовать все значения ChatAction по документации Telegram
    /// </summary>
    [Theory(DisplayName = "Тест 1: Должен сериализовать и десериализовать все значения ChatAction по документации Telegram")]
    [MemberData(nameof(Data))]
    public void Should_SerializeAndDeserialize_AsInTelegramDocs(ChatAction action, string expected)
    {
        var json = JsonSerializer.Serialize(action, Json);
        var restored = JsonSerializer.Deserialize<ChatAction>(json, Json);

        json.Should().Be($"\"{expected}\"");
        restored.Should().Be(action);
    }

    public static IEnumerable<object[]> Data()
    {
        yield return new object[] { ChatAction.Typing, "typing" };
        yield return new object[] { ChatAction.UploadPhoto, "upload_photo" };
        yield return new object[] { ChatAction.RecordVideo, "record_video" };
        yield return new object[] { ChatAction.UploadVideo, "upload_video" };
        yield return new object[] { ChatAction.RecordVoice, "record_voice" };
        yield return new object[] { ChatAction.UploadVoice, "upload_voice" };
        yield return new object[] { ChatAction.UploadDocument, "upload_document" };
        yield return new object[] { ChatAction.FindLocation, "find_location" };
        yield return new object[] { ChatAction.RecordVideoNote, "record_video_note" };
        yield return new object[] { ChatAction.UploadVideoNote, "upload_video_note" };
        yield return new object[] { ChatAction.ChooseSticker, "choose_sticker" };
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };
}

