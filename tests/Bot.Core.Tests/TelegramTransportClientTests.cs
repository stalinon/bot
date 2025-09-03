using System.IO;
using System.Reflection;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Telegram;

using Moq;

using Telegram.Bot;

using Xunit;

using TelegramChatAction = global::Telegram.Bot.Types.Enums.ChatAction;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты клиента Telegram.
/// </summary>
public class TelegramTransportClientTests
{
    private static readonly MethodInfo MapMethod = typeof(TelegramTransportClient)
        .GetMethod("Map", BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Проверяет сопоставление действий.
    /// </summary>
    [Theory(DisplayName = "Тест 1. Сопоставление ChatAction")]
    [InlineData(ChatAction.Typing, TelegramChatAction.Typing)]
    [InlineData(ChatAction.UploadPhoto, TelegramChatAction.UploadPhoto)]
    [InlineData(ChatAction.RecordVideo, TelegramChatAction.RecordVideo)]
    [InlineData(ChatAction.UploadVideo, TelegramChatAction.UploadVideo)]
    [InlineData(ChatAction.RecordVoice, TelegramChatAction.RecordVoice)]
    [InlineData(ChatAction.UploadVoice, TelegramChatAction.UploadVoice)]
    [InlineData(ChatAction.UploadDocument, TelegramChatAction.UploadDocument)]
    [InlineData(ChatAction.FindLocation, TelegramChatAction.FindLocation)]
    [InlineData(ChatAction.RecordVideoNote, TelegramChatAction.RecordVideoNote)]
    [InlineData(ChatAction.UploadVideoNote, TelegramChatAction.UploadVideoNote)]
    [InlineData(ChatAction.ChooseSticker, TelegramChatAction.ChooseSticker)]
    public void MapChatAction(ChatAction action, TelegramChatAction expected)
    {
        var result = (TelegramChatAction)MapMethod.Invoke(null, new object[] { action })!;
        Assert.Equal(expected, result);
    }

    /// <summary>
    ///     Проверяет отмену отправки текста.
    /// </summary>
    [Fact(DisplayName = "Тест 2. Отмена отправки текста")]
    public async Task SendTextAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.SendTextAsync(new ChatAddress(1), "test", cts.Token));
    }

    /// <summary>
    ///     Проверяет отмену отправки фото.
    /// </summary>
    [Fact(DisplayName = "Тест 3. Отмена отправки фото")]
    public async Task SendPhotoAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.SendPhotoAsync(new ChatAddress(1), Stream.Null, null, cts.Token));
    }

    /// <summary>
    ///     Проверяет отмену редактирования текста.
    /// </summary>
    [Fact(DisplayName = "Тест 4. Отмена редактирования текста")]
    public async Task EditMessageTextAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.EditMessageTextAsync(new ChatAddress(1), 1, "", cts.Token));
    }

    /// <summary>
    ///     Проверяет отмену редактирования подписи.
    /// </summary>
    [Fact(DisplayName = "Тест 5. Отмена редактирования подписи")]
    public async Task EditMessageCaptionAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.EditMessageCaptionAsync(new ChatAddress(1), 1, null, cts.Token));
    }

    /// <summary>
    ///     Проверяет отмену отправки действия.
    /// </summary>
    [Fact(DisplayName = "Тест 6. Отмена отправки действия")]
    public async Task SendChatActionAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.SendChatActionAsync(new ChatAddress(1), ChatAction.Typing, cts.Token));
    }

    /// <summary>
    ///     Проверяет отмену удаления сообщения.
    /// </summary>
    [Fact(DisplayName = "Тест 7. Отмена удаления сообщения")]
    public async Task DeleteMessageAsync_Cancel()
    {
        var tx = new TelegramTransportClient(Mock.Of<ITelegramBotClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tx.DeleteMessageAsync(new ChatAddress(1), 1, cts.Token));
    }
}

