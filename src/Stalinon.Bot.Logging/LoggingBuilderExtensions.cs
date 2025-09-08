using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Stalinon.Bot.Logging;

/// <summary>
///     Методы расширения для подключения логирования.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Регистрируют форматтер и фильтры.</item>
///         <item>Читают уровень логирования из переменной окружения <c>LOG_LEVEL</c>.</item>
///     </list>
/// </remarks>
public static class LoggingBuilderExtensions
{
    /// <summary>
    ///     Добавить логирование бота.
    /// </summary>
    /// <param name="configure">Дополнительная настройка параметров.</param>
    public static ILoggingBuilder AddBotLogging(this ILoggingBuilder builder, Action<BotLoggerOptions>? configure = null)
    {
        builder.AddConsole(o => o.FormatterName = BotConsoleFormatter.FormatterName);
        builder.AddConsoleFormatter<BotConsoleFormatter, BotLoggerOptions>();
        builder.AddFilter("Microsoft", LogLevel.Warning);
        builder.AddFilter("System", LogLevel.Warning);

        var levelVar = Environment.GetEnvironmentVariable("LOG_LEVEL");
        if (Enum.TryParse<LogLevel>(levelVar, true, out var level))
        {
            builder.SetMinimumLevel(level);
        }

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
