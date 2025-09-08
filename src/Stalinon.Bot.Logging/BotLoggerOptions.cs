using System.Collections.Generic;

using Microsoft.Extensions.Logging.Console;

namespace Stalinon.Bot.Logging;

/// <summary>
///     Параметры логирования бота.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет длину строковых полей.</item>
///         <item>Задаёт параметры сэмплирования.</item>
///     </list>
/// </remarks>
public sealed class BotLoggerOptions : ConsoleFormatterOptions
{
    /// <summary>
    ///     Максимальная длина строковых полей.
    /// </summary>
    public int MaxFieldLength { get; set; } = 1024;

    /// <summary>
    ///     Доли записей для шумных сообщений по тексту сообщения.
    /// </summary>
    public Dictionary<string, double> Sampling { get; } = new();
}
