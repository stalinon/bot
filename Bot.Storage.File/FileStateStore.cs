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
    private readonly string[] _prefix;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Timer _cleaner;
    
    ///  <inheritdoc cref="FileStateStore"/>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        _prefix = string.IsNullOrWhiteSpace(options.Prefix) ? Array.Empty<string>() : Norm(options.Prefix);
        Directory.CreateDirectory(Path.Combine(new[] { _basePath }.Concat(_prefix).ToArray()));
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
        var dir = DirFor(scope, key);
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"{SanLast(key)}.tmp");
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

    private static string[] Norm(string s) => s.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(San).ToArray();
    private static string SanLast(string s) => Norm(s).Last();
    private static string San(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_'));
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
