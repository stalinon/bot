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
    ///     Сохранить контекст текущего шага.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    /// <param name="data">Произвольные данные шага.</param>
    /// <param name="ttl">Время жизни шага.</param>
    Task SaveStepAsync(UpdateContext ctx, string? data = null, TimeSpan? ttl = null);

    /// <summary>
    ///     Перейти к следующему шагу.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    /// <param name="data">Произвольные данные шага.</param>
    /// <param name="ttl">Время жизни шага.</param>
    Task<int> NextStepAsync(UpdateContext ctx, string? data = null, TimeSpan? ttl = null);

    /// <summary>
    ///     Установить номер шага.
    /// </summary>
    /// <param name="ctx">Контекст обновления.</param>
    /// <param name="step">Новый номер шага.</param>
    /// <param name="data">Произвольные данные шага.</param>
    /// <param name="ttl">Время жизни шага.</param>
    Task SetStepAsync(UpdateContext ctx, int step, string? data = null, TimeSpan? ttl = null);
}
