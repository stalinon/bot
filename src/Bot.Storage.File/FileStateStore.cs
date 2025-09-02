using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using Bot.Abstractions.Contracts;
using Bot.Storage.File.Options;

namespace Bot.Storage.File;

/// <summary>
///     Файловое хранилище
/// </summary>
public sealed class FileStateStore : IStateStorage, IAsyncDisposable
{
    private readonly string _basePath;
    private readonly string[] _prefix;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, byte[]>? _buffer;
    private readonly Timer? _flushTimer;
    private readonly Timer _cleaner;

    /// <summary>
    ///     Создаёт файловое хранилище
    /// </summary>
    /// <param name="options">Настройки</param>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        _prefix = string.IsNullOrWhiteSpace(options.Prefix) ? Array.Empty<string>() : Norm(options.Prefix);
        Directory.CreateDirectory(Path.Combine(new[] { _basePath }.Concat(_prefix).ToArray()));
        if (options.BufferHotKeys)
        {
            _buffer = new();
            _flushTimer = new Timer(Flush, null, options.FlushPeriod, options.FlushPeriod);
        }
        var period = options.CleanUpPeriod;
        _cleaner = new Timer(_ => CleanUpExpired(), null, period, period);
    }

    /// <summary>
    ///     Получить значение
    /// </summary>
    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        var meta = MetaPathFor(scope, key);
        if (_buffer is not null && _buffer.TryGetValue(file, out var data))
        {
            return JsonSerializer.Deserialize<T>(data, Json);
        }

        if (!System.IO.File.Exists(file))
        {
            return default;
        }

        if (System.IO.File.Exists(meta))
        {
            var txt = await System.IO.File.ReadAllTextAsync(meta, ct);
            if (long.TryParse(txt, out var ms) && DateTimeOffset.FromUnixTimeMilliseconds(ms) <= DateTimeOffset.UtcNow)
            {
                System.IO.File.Delete(meta);
                System.IO.File.Delete(file);
                return default;
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
        var meta = MetaPathFor(scope, key);
        if (_buffer is null)
        {
            var dir = DirFor(scope, key);
            Directory.CreateDirectory(dir);
            var tmp = Path.Combine(dir, $"{SanLast(key)}.tmp");
            await using (var fs = System.IO.File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(fs, value, Json, ct);
            }

            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }

            System.IO.File.Move(tmp, file);
            if (ttl is null)
            {
                if (System.IO.File.Exists(meta))
                {
                    System.IO.File.Delete(meta);
                }
            }
            else
            {
                var expires = DateTimeOffset.UtcNow.Add(ttl.Value).ToUnixTimeMilliseconds();
                await System.IO.File.WriteAllTextAsync(meta, expires.ToString(), ct);
            }

            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        _buffer[file] = data;
    }

    /// <summary>
    ///     Удалить значение
    /// </summary>
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        var meta = MetaPathFor(scope, key);

        var removed = _buffer?.TryRemove(file, out _) ?? false;
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
            System.IO.File.Delete(meta);
            return Task.FromResult(true);
        }

        return Task.FromResult(removed);
    }

    /// <summary>
    ///     Увеличить числовое значение
    /// </summary>
    public async Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var current = await GetAsync<long>(scope, key, ct).ConfigureAwait(false);
        current += value;
        await SetAsync(scope, key, current, ttl, ct).ConfigureAwait(false);
        return current;
    }

    /// <summary>
    ///     Установить значение, если ключ отсутствует
    /// </summary>
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var existing = await GetAsync<T>(scope, key, ct).ConfigureAwait(false);
        if (existing is not null && !existing.Equals(default(T)))
        {
            return false;
        }
        await SetAsync(scope, key, value, ttl, ct).ConfigureAwait(false);
        return true;
    }

    private string DirFor(string scope, string key = "")
    {
        var parts = new List<string> { _basePath };
        parts.AddRange(_prefix);
        parts.AddRange(Norm(scope));
        if (!string.IsNullOrEmpty(key))
        {
            var kp = Norm(key);
            if (kp.Length > 1)
            {
                parts.AddRange(kp[..^1]);
            }
        }

        return Path.Combine(parts.ToArray());
    }

    private string PathFor(string scope, string key)
    {
        var dir = DirFor(scope, key);
        return Path.Combine(dir, $"{SanLast(key)}.json");
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
    ///     Очистка просроченных ключей.
    /// </summary>
    private void CleanUpExpired()
    {
        foreach (var dir in Directory.EnumerateDirectories(_basePath))
        {
            foreach (var meta in Directory.EnumerateFiles(dir, "*.meta"))
            {
                try
                {
                    var txt = System.IO.File.ReadAllText(meta);
                    if (!long.TryParse(txt, out var ms))
                    {
                        continue;
                    }

                    if (DateTimeOffset.FromUnixTimeMilliseconds(ms) > DateTimeOffset.UtcNow)
                    {
                        continue;
                    }

                    var json = Path.ChangeExtension(meta, ".json");
                    if (System.IO.File.Exists(json))
                    {
                        System.IO.File.Delete(json);
                    }

                    System.IO.File.Delete(meta);
                }
                catch
                {
                    // проигнорировать ошибки
                }
            }
        }
    }

    /// <summary>
    ///     Освободить ресурсы и сбросить буфер
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Flush(null);
        _cleaner.Dispose();
        _flushTimer?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string SanLast(string s) => Norm(s).Last();
    private static string[] Norm(string s) => s.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(San).ToArray();
    private string MetaPathFor(string scope, string key) => Path.Combine(DirFor(scope), $"{San(key)}.meta");
    private static string San(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_'));
}
