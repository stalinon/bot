using System.Text.Json;
using Bot.Abstractions.Contracts;
using Bot.Storage.File.Options;
using System;
using System.Threading;

namespace Bot.Storage.File;

/// <summary>
///     Файловое хранилище
/// </summary>
public sealed class FileStateStore : IStateStore, IDisposable
{
    private readonly string _basePath;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Timer _cleaner;
    
    ///  <inheritdoc cref="FileStateStore"/>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        Directory.CreateDirectory(_basePath);
        var period = options.CleanUpPeriod;
        _cleaner = new Timer(_ => CleanUpExpired(), null, period, period);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        var meta = MetaPathFor(scope, key);
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
    
    /// <inheritdoc />
    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dir = DirFor(scope);
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"{San(key)}.tmp");
        var file = PathFor(scope, key);
        var meta = MetaPathFor(scope, key);
        await using (var fs = System.IO.File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, value, Json, ct);
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
    }
    
    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        var meta = MetaPathFor(scope, key);
        if (!System.IO.File.Exists(file))
        {
            return Task.FromResult(false);
        }

        System.IO.File.Delete(file);
        if (System.IO.File.Exists(meta))
        {
            System.IO.File.Delete(meta);
        }
        return Task.FromResult(true);
    }

    private string DirFor(string scope) => Path.Combine(_basePath, San(scope));
    private string PathFor(string scope, string key) => Path.Combine(DirFor(scope), $"{San(key)}.json");
    private string MetaPathFor(string scope, string key) => Path.Combine(DirFor(scope), $"{San(key)}.meta");
    private static string San(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));

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
    ///     Освобождение ресурсов таймера.
    /// </summary>
    public void Dispose() => _cleaner.Dispose();
}
