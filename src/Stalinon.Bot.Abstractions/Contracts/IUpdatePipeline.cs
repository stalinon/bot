namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Пайплайн обновления
/// </summary>
public interface IUpdatePipeline
{
    /// <summary>
    ///     Использовать мидлварь
    /// </summary>
    IUpdatePipeline Use<T>() where T : IUpdateMiddleware;

    /// <summary>
    ///     Использовать мидлварь
    /// </summary>
    IUpdatePipeline Use<T>(T middleware) where T : IUpdateMiddleware;

    /// <summary>
    ///     Использовать преобразование
    /// </summary>
    IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component);

    /// <summary>
    ///     Построить
    /// </summary>
    UpdateDelegate Build(UpdateDelegate terminal);
}
