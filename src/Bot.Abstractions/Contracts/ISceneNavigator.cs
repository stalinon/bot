namespace Bot.Abstractions.Contracts;

using System;

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
    /// <param name="data">Произвольные данные шага.</param>
    /// <param name="ttl">Время жизни шага.</param>
    Task<int> NextStepAsync(UpdateContext ctx, string? data = null, TimeSpan? ttl = null);
}
