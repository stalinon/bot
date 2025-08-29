using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;

using Bot.Abstractions.Contracts;
using Bot.Storage.File.Options;
using System;
using System.Threading;

namespace Bot.Storage.File;

/// <summary>
///     Файловое хранилище
/// </summary>
public sealed class FileStateStore : IStateStore, IAsyncDisposable
{
    private readonly string _basePath;
    private readonly string[] _prefix;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, byte[]>? _buffer;
    private readonly Timer? _flushTimer;

    /// <summary>
    ///     Создаёт файловое хранилище
    /// </summary>
    /// <param name="options">Настройки</param>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        Directory.CreateDirectory(_basePath);
        if (options.BufferHotKeys)
        {
            _buffer = new();
            _flushTimer = new Timer(Flush, null, options.FlushPeriod, options.FlushPeriod);
        }
    }

    /// <summary>
    ///     Получить значение
    /// </summary>
    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        if (_buffer is not null && _buffer.TryGetValue(file, out var data))
        {
            return JsonSerializer.Deserialize<T>(data, Json);
        }

        if (!System.IO.File.Exists(file))
        {
            return default(T?);
        }

        if (System.IO.File.Exists(meta))
        {
            var txt = await System.IO.File.ReadAllTextAsync(meta, ct);
            if (long.TryParse(txt, out var ms) && DateTimeOffset.FromUnixTimeMilliseconds(ms) <= DateTimeOffset.UtcNow)
            {
                System.IO.File.Delete(meta);
                System.IO.File.Delete(file);
                return default(T?);
            }
        }

        await using var fs = System.IO.File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<T>(fs, Json, ct);
    }

    /// <summary>
    ///     Установить значение
    /// </summary>
    /// <inheritdoc />
    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        if (_buffer is null)
        {
            var dir = DirFor(scope);
            Directory.CreateDirectory(dir);
            var tmp = Path.Combine(dir, $"{San(key)}.tmp");
            await using (var fs = System.IO.File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(fs, value, Json, ct);
            }
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }

            System.IO.File.Move(tmp, file);
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        _buffer[file] = data;
    }

    /// <summary>
    ///     Удалить значение
    /// </summary>
    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        var removed = _buffer?.TryRemove(file, out _) ?? false;
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
            return Task.FromResult(true);
        }

        return Task.FromResult(removed);
    }

    private void Flush(object? _)
    {
        if (_buffer is null)
        {
            return;
        }

        foreach (var (file, _) in _buffer.ToArray())
        {
            if (!_buffer.TryGetValue(file, out var payload))
            {
                continue;
            }

            var dir = Path.GetDirectoryName(file)!;
            Directory.CreateDirectory(dir);
            var tmp = $"{file}.tmp";
            System.IO.File.WriteAllBytes(tmp, payload);
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }

            System.IO.File.Move(tmp, file);
            _buffer.TryRemove(new KeyValuePair<string, byte[]>(file, payload));
        }
    }

    /// <summary>
    ///     Освободить ресурсы и сбросить буфер
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Flush(null);
        _flushTimer?.Dispose();
        return ValueTask.CompletedTask;
    }

    private string DirFor(string scope) => Path.Combine(_basePath, San(scope));
    private string PathFor(string scope, string key) => Path.Combine(DirFor(scope), $"{San(key)}.json");
    private static string San(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
}
