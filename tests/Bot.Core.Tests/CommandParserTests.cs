using System.Collections.Generic;

using Bot.Core.Middlewares;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты парсера команд.
/// </summary>
public class CommandParserTests
{
    /// <summary>
    ///     Набор тестовых сценариев.
    /// </summary>
    public static IEnumerable<object?[]> Cases => new object?[][]
    {
        new object?[]{"/cmd", "/cmd", null, Array.Empty<string>()},
        new object?[]{"/cmd@Bot", "/cmd", null, Array.Empty<string>()},
        new object?[]{"/cmd arg", "/cmd", "arg", new[]{"arg"}},
        new object?[]{"/cmd arg1 arg2", "/cmd", "arg1 arg2", new[]{"arg1","arg2"}},
        new object?[]{"/cmd   arg1   arg2", "/cmd", "arg1   arg2", new[]{"arg1","arg2"}},
        new object?[]{"/cmd \"arg 1\"", "/cmd", "\"arg 1\"", new[]{"arg 1"}},
        new object?[]{"/cmd \"arg 1\" arg2", "/cmd", "\"arg 1\" arg2", new[]{"arg 1","arg2"}},
        new object?[]{"/cmd 'arg 1'", "/cmd", "'arg 1'", new[]{"arg 1"}},
        new object?[]{"/cmd \"arg with \\\"quotes\\\"\"", "/cmd", "\"arg with \\\"quotes\\\"\"", new[]{"arg with \"quotes\""}},
        new object?[]{"/cmd@Bot arg", "/cmd", "arg", new[]{"arg"}},
        new object?[]{"/cmd   ", "/cmd", "", Array.Empty<string>()},
        new object?[]{"/cmd arg1 \"arg 2\" 'arg 3'", "/cmd", "arg1 \"arg 2\" 'arg 3'", new[]{"arg1","arg 2","arg 3"}},
        new object?[]{"/cmd \"\" arg", "/cmd", "\"\" arg", new[]{"arg"}},
        new object?[]{"/cmd \"arg with \\\"quotes\\\"\" arg2", "/cmd", "\"arg with \\\"quotes\\\"\" arg2", new[]{"arg with \"quotes\"","arg2"}},
        new object?[]{"text", null, null, null},
    };

    /// <summary>
    ///     Проверяет корректность разбора текста команды.
    /// </summary>
    [Theory(DisplayName = "Тест 1. Парсинг команд")]
    [MemberData(nameof(Cases))]
    public void Parse(string text, string? cmd, string? payload, string[]? expectedArgs)
    {
        var result = CommandParser.Parse(text);
        if (cmd is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Equal(cmd, result!.Command);
            Assert.Equal(payload, result.Payload);
            Assert.Equal(expectedArgs!, result.Args);
        }
    }
}
