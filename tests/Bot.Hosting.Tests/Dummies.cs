using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Hosting.Tests;

/// <summary>
///     Заглушка источника обновлений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Не генерирует реальные обновления</item>
///         <item>Применяется только в тестах</item>
///     </list>
/// </remarks>
internal sealed class DummyUpdateSource : IUpdateSource
{
    /// <summary>
    ///     Запуск заглушки.
    /// </summary>
    public Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
///     Заглушка хранилища состояний.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Не сохраняет данные</item>
///         <item>Используется для изоляции тестов</item>
///     </list>
/// </remarks>
internal sealed class DummyStateStore : IStateStore
{
    /// <summary>
    ///     Заглушка получения.
    /// </summary>
    public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        return Task.FromResult(default(T));
    }

    /// <summary>
    ///     Заглушка установки.
    /// </summary>
    public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Заглушка удаления.
    /// </summary>
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Заглушка инкремента.
    /// </summary>
    public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        return Task.FromResult(0L);
    }

    /// <summary>
    ///     Заглушка условной установки.
    /// </summary>
    public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Заглушка сравнения и установки.
    /// </summary>
    public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl,
        CancellationToken ct)
    {
        return Task.FromResult(true);
    }
}

/// <summary>
///     Заглушка построителя пайплайна.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Не добавляет реальные промежуточные слои</item>
///         <item>Применяется в тестах</item>
///     </list>
/// </remarks>
internal sealed class DummyPipeline : IUpdatePipeline
{
    /// <summary>
    ///     Заглушка добавления промежуточного слоя.
    /// </summary>
    public IUpdatePipeline Use<T>() where T : IUpdateMiddleware
    {
        return this;
    }

    /// <summary>
    ///     Заглушка добавления компонента.
    /// </summary>
    public IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component)
    {
        return this;
    }

    /// <summary>
    ///     Заглушка сборки.
    /// </summary>
    public UpdateDelegate Build(UpdateDelegate terminal)
    {
        return terminal;
    }
}
