namespace Bot.Abstractions.Contracts;

/// <summary>
///     Навигатор по сценам.
/// </summary>
public interface ISceneNavigator
{
    /// <summary>
    ///     Войти в сцену.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    /// <param name="scene">Сцена.</param>
    Task EnterAsync(UpdateContext ctx, IScene scene);

    /// <summary>
    ///     Выйти из сцены.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    Task ExitAsync(UpdateContext ctx);

    /// <summary>
    ///     Получить состояние.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    Task<SceneState?> GetStateAsync(UpdateContext ctx);

    /// <summary>
    ///     Перейти к следующему шагу.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    Task<int> NextStepAsync(UpdateContext ctx);
}
