using Bot.Core.Middlewares;

using FluentAssertions;

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
    public static IEnumerable<object?[]> Cases => new[]
    {
        new object?[] { "/cmd", "cmd", null, Array.Empty<string>() },
        new object?[] { "/cmd@Bot", "cmd", null, Array.Empty<string>() },
        new object?[] { "/cmd arg", "cmd", "arg", new[] { "arg" } },
        new object?[] { "/cmd arg1 arg2", "cmd", "arg1 arg2", new[] { "arg1", "arg2" } },
        new object?[] { "/cmd   arg1   arg2", "cmd", "arg1   arg2", new[] { "arg1", "arg2" } },
        new object?[] { "/cmd \"arg 1\"", "cmd", "\"arg 1\"", new[] { "arg 1" } },
        new object?[] { "/cmd \"arg 1\" arg2", "cmd", "\"arg 1\" arg2", new[] { "arg 1", "arg2" } },
        new object?[] { "/cmd 'arg 1'", "cmd", "'arg 1'", new[] { "arg 1" } },
        new object?[]
        {
            "/cmd \"arg with \\\"quotes\\\"\"", "cmd", "\"arg with \\\"quotes\\\"\"", new[] { "arg with \"quotes\"" }
        },
        new object?[] { "/cmd@Bot arg", "cmd", "arg", new[] { "arg" } },
        new object?[] { "/cmd   ", "cmd", "", Array.Empty<string>() },
        new object?[]
            { "/cmd arg1 \"arg 2\" 'arg 3'", "cmd", "arg1 \"arg 2\" 'arg 3'", new[] { "arg1", "arg 2", "arg 3" } },
        new object?[] { "/cmd \"\" arg", "cmd", "\"\" arg", new[] { "arg" } },
        new object?[]
        {
            "/cmd \"arg with \\\"quotes\\\"\" arg2", "cmd", "\"arg with \\\"quotes\\\"\" arg2",
            new[] { "arg with \"quotes\"", "arg2" }
        },
        new object?[] { "text", null, null, null }
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
            result.Should().BeNull();
        }
        else
        {
            result.Should().NotBeNull();
            result!.Command.Should().Be(cmd);
            result.Payload.Should().Be(payload);
            result.Args.Should().Equal(expectedArgs!);
        }
    }

    /// <summary>
    ///     Тест 2. Должен поддерживать подкоманды
    /// </summary>
    [Fact(DisplayName = "Тест 2. Должен поддерживать подкоманды")]
    public void Should_Parse_Subcommands()
    {
        var result = CommandParser.Parse("/vote tax 5");
        result.Should().NotBeNull();
        result!.Parts.Should().Equal("vote", "tax", "5");
    }
}
