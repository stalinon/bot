using System.IO;
using System.Threading.Tasks;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Hosting.Tests;

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

    /// <summary>
    ///     Остановка заглушки.
    /// </summary>
    public Task StopAsync()
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
    ///     Заглушка добавления промежуточного слоя.
    /// </summary>
    public IUpdatePipeline Use<T>(T middleware) where T : IUpdateMiddleware
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

/// <summary>
///     Заглушка транспортного клиента.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Не выполняет реальные отправки.</item>
///     </list>
/// </remarks>
internal sealed class DummyTransportClient : ITransportClient
{
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct) => Task.CompletedTask;
    public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct) => Task.CompletedTask;
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct) => Task.CompletedTask;
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct) => Task.CompletedTask;
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct) => Task.CompletedTask;
}
