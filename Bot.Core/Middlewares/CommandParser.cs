using System.Text;

namespace Bot.Core.Middlewares;

/// <summary>
///     Утилита для разбора команд с аргументами и кавычками.
/// </summary>
public static class CommandParser
{
    /// <summary>
    ///     Разбирает текст на команду, исходный payload и аргументы.
    /// </summary>
    public static CommandParseResult? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
        {
            return null;
        }

        var spaceIndex = text.IndexOf(' ');
        var cmdPart = spaceIndex < 0 ? text : text[..spaceIndex];
        var atIndex = cmdPart.IndexOf('@');
        var cmd = atIndex < 0 ? cmdPart : cmdPart[..atIndex];

        var payload = spaceIndex < 0 ? null : text[(spaceIndex + 1)..].Trim();
        var args = payload is null ? Array.Empty<string>() : SplitArguments(payload);

        return new CommandParseResult(cmd, payload, args);
    }

    /// <summary>
    ///     Разбивает строку полезной нагрузки на аргументы.
    /// </summary>
    private static string[] SplitArguments(string payload)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        var state = ParserState.None;
        char quote = '\0';

        foreach (var c in payload)
        {
            switch (state)
            {
                case ParserState.None:
                    if (char.IsWhiteSpace(c))
                    {
                        AddArgument();
                    }
                    else if (c == '"' || c == '\'')
                    {
                        state = ParserState.Quoted;
                        quote = c;
                    }
                    else
                    {
                        current.Append(c);
                    }
                    break;
                case ParserState.Quoted:
                    if (c == '\\')
                    {
                        state = ParserState.Escape;
                    }
                    else if (c == quote)
                    {
                        state = ParserState.None;
                    }
                    else
                    {
                        current.Append(c);
                    }
                    break;
                case ParserState.Escape:
                    current.Append(c);
                    state = ParserState.Quoted;
                    break;
            }
        }

        AddArgument();
        return args.ToArray();

        void AddArgument()
        {
            if (current.Length > 0)
            {
                args.Add(current.ToString());
                current.Clear();
            }
        }
    }

    private enum ParserState
    {
        None,
        Quoted,
        Escape
    }
}
