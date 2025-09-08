namespace Stalinon.Bot.Abstractions.Addresses;

/// <summary>
///     Адрес чата
/// </summary>
/// <param name="Id">Идентификатор</param>
/// <param name="Type">Тип</param>
public readonly record struct ChatAddress(long Id, string Type = "private");
