namespace Bot.Abstractions;

/// <summary>
///     Действие бота в чате.
/// </summary>
public enum ChatAction
{
    /// <summary>
    ///     Печатает текст.
    /// </summary>
    Typing,

    /// <summary>
    ///     Загружает фото.
    /// </summary>
    UploadPhoto,

    /// <summary>
    ///     Записывает видео.
    /// </summary>
    RecordVideo,

    /// <summary>
    ///     Загружает видео.
    /// </summary>
    UploadVideo,

    /// <summary>
    ///     Записывает голосовое сообщение.
    /// </summary>
    RecordVoice,

    /// <summary>
    ///     Загружает голосовое сообщение.
    /// </summary>
    UploadVoice,

    /// <summary>
    ///     Загружает документ.
    /// </summary>
    UploadDocument,

    /// <summary>
    ///     Ищет геопозицию.
    /// </summary>
    FindLocation,

    /// <summary>
    ///     Записывает видеосообщение.
    /// </summary>
    RecordVideoNote,

    /// <summary>
    ///     Загружает видеосообщение.
    /// </summary>
    UploadVideoNote,

    /// <summary>
    ///     Выбирает стикер.
    /// </summary>
    ChooseSticker,
}

