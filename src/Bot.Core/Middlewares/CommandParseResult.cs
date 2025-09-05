namespace Bot.Core.Middlewares;

/// <summary>
///     Результат разбора команды.
/// </summary>
/// <param name="Command">Имя команды без ведущего слеша.</param>
/// <param name="Payload">Исходная строка аргументов после команды.</param>
/// <param name="Args">Массив разобранных аргументов.</param>
public sealed record CommandParseResult(string Command, string? Payload, string[] Args)
{
    /// <summary>
    ///     Все части команды и аргументы.
    /// </summary>
    public string[] Parts => [Command, .. Args];
}
