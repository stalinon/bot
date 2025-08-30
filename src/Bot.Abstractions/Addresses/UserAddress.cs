namespace Bot.Abstractions.Addresses;

/// <summary>
///     Адрес пользователя
/// </summary>
/// <param name="Id">Идентификатор</param>
/// <param name="Username">Ник</param>
/// <param name="LanguageCode">Код языка</param>
public readonly record struct UserAddress(long Id, string? Username = null, string? LanguageCode = null);