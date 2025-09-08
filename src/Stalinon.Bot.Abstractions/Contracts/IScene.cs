namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Сцена
/// </summary>
public interface IScene
{
    /// <summary>
    ///     Название
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Есть ли доступ
    /// </summary>
    Task<bool> CanEnter(UpdateContext ctx);

    /// <summary>
    ///     На вход
    /// </summary>
    Task OnEnter(UpdateContext ctx);

    /// <summary>
    ///     На обновление
    /// </summary>
    Task OnUpdate(UpdateContext ctx);
}
