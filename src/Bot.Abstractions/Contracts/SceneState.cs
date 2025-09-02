namespace Bot.Abstractions.Contracts;

using Bot.Abstractions.Addresses;

/// <summary>
///     Состояние сцены.
/// </summary>
/// <param name="User">Пользователь.</param>
/// <param name="Chat">Чат.</param>
/// <param name="Scene">Название сцены.</param>
/// <param name="Step">Текущий шаг.</param>
public sealed record SceneState(UserAddress User, ChatAddress Chat, string Scene, int Step);
