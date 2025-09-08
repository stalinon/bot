using Stalinon.Bot.Abstractions.Addresses;

namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Состояние сцены.
/// </summary>
/// <param name="User">Пользователь.</param>
/// <param name="Chat">Чат.</param>
/// <param name="Scene">Название сцены.</param>
/// <param name="Step">Текущий шаг.</param>
/// <param name="Data">Произвольные данные шага.</param>
/// <param name="UpdatedAt">Время последнего обновления.</param>
/// <param name="Ttl">Время жизни шага.</param>
public sealed record SceneState(
    UserAddress User,
    ChatAddress Chat,
    string Scene,
    int Step,
    string? Data,
    DateTimeOffset UpdatedAt,
    TimeSpan? Ttl);
