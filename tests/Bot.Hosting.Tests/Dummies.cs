using System;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Hosting.Tests;

/// <summary>
///     Заглушка источника обновлений.
/// </summary>
internal sealed class DummyUpdateSource : IUpdateSource
{
    /// <summary>
    ///     Запуск заглушки.
    /// </summary>
    public Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
///     Заглушка хранилища состояний.
/// </summary>
internal sealed class DummyStateStorage : IStateStorage
{
    /// <summary>
    ///     Заглушка получения.
    /// </summary>
    public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct) => Task.FromResult(default(T));

    /// <summary>
    ///     Заглушка установки.
    /// </summary>
    public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка удаления.
    /// </summary>
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct) => Task.FromResult(true);

    /// <summary>
    ///     Заглушка инкремента.
    /// </summary>
    public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct) => Task.FromResult(0L);

    /// <summary>
    ///     Заглушка условной установки.
    /// </summary>
    public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct) => Task.FromResult(true);
}

/// <summary>
///     Заглушка построителя пайплайна.
/// </summary>
internal sealed class DummyPipeline : IUpdatePipeline
{
    /// <summary>
    ///     Заглушка добавления промежуточного слоя.
    /// </summary>
    public IUpdatePipeline Use<T>() where T : IUpdateMiddleware => this;

    /// <summary>
    ///     Заглушка добавления компонента.
    /// </summary>
    public IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component) => this;

    /// <summary>
    ///     Заглушка сборки.
    /// </summary>
    public UpdateDelegate Build(UpdateDelegate terminal) => terminal;
}

