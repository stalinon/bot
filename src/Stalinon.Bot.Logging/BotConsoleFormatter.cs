using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Stalinon.Bot.Logging;

/// <summary>
///     Форматтер логов бота с усечением и маскированием.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Ограничивает длину строковых полей.</item>
///         <item>Маскирует токены и JWT.</item>
///     </list>
/// </remarks>
public sealed class BotConsoleFormatter(IOptionsMonitor<BotLoggerOptions> options) : ConsoleFormatter(FormatterName)
{
    /// <summary>
    ///     Имя форматтера.
    /// </summary>
    public const string FormatterName = "bot";

    private readonly IOptionsMonitor<BotLoggerOptions> _options = options;
    private readonly Random _random = new();

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var opts = _options.CurrentValue;
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (opts.Sampling.TryGetValue(message, out var rate) && rate < 1 && _random.NextDouble() > rate)
        {
            return;
        }

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writer.WriteString("Level", logEntry.LogLevel.ToString());
        writer.WriteString("Message", Limit(Mask("Message", message), opts.MaxFieldLength));

        if (logEntry.State is IEnumerable<KeyValuePair<string, object?>> state)
        {
            writer.WriteStartObject("State");
            foreach (var (key, value) in state)
            {
                if (value is null)
                {
                    continue;
                }

                writer.WriteString(key, Limit(Mask(key, value.ToString()), opts.MaxFieldLength));
            }

            writer.WriteEndObject();
        }

        if (scopeProvider is not null)
        {
            writer.WriteStartObject("Scope");
            scopeProvider.ForEachScope((scope, json) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                {
                    foreach (var (key, value) in kvps)
                    {
                        if (value is null)
                        {
                            continue;
                        }

                        json.WriteString(key, Limit(Mask(key, value.ToString()), opts.MaxFieldLength));
                    }
                }
            }, writer);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();
        textWriter.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }

    private static string Limit(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length > max ? value[..max] : value;
    }

    private static string Mask(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)
            || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
            || key.Contains("JWT", StringComparison.OrdinalIgnoreCase)
            || IsJwt(value))
        {
            return "***";
        }

        return value;
    }

    private static bool IsJwt(string value)
    {
        return value.Count(c => c == '.') == 2;
    }
}
