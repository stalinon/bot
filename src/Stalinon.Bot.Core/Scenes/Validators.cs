namespace Stalinon.Bot.Core.Scenes;

/// <summary>
///     Стандартные валидаторы ответов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверка на непустую строку.</item>
///         <item>Проверка на целое число.</item>
///     </list>
/// </remarks>
public static class Validators
{
    /// <summary>
    ///     Проверить, что строка не пуста.
    /// </summary>
    /// <param name="text">Текст ответа.</param>
    /// <returns><c>true</c>, если строка непуста.</returns>
    public static bool NotEmpty(string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }

    /// <summary>
    ///     Проверить, что строка представляет целое число.
    /// </summary>
    /// <param name="text">Текст ответа.</param>
    /// <returns><c>true</c>, если текст — целое число.</returns>
    public static bool IsInt(string text)
    {
        return int.TryParse(text, out _);
    }
}
