using System.Text.Json;
using Bot.Abstractions.Contracts;
using Bot.Storage.File.Options;

namespace Bot.Storage.File;

/// <summary>
///     Файловое хранилище
/// </summary>
public sealed class FileStateStore : IStateStore
{
    private readonly string _basePath;
    private readonly string[] _prefix;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    ///  <inheritdoc cref="FileStateStore"/>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        _prefix = string.IsNullOrWhiteSpace(options.Prefix) ? Array.Empty<string>() : Norm(options.Prefix);
        Directory.CreateDirectory(Path.Combine(new[] { _basePath }.Concat(_prefix).ToArray()));
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        if (!System.IO.File.Exists(file))
        {
            return default;
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
        await using (var fs = System.IO.File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, value, Json, ct);
        if (System.IO.File.Exists(file))
        {
            System.IO.File.Delete(file);
        }

        System.IO.File.Move(tmp, file);
    }
    
    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var file = PathFor(scope, key);
        if (!System.IO.File.Exists(file))
        {
            return Task.FromResult(false);
        }

        System.IO.File.Delete(file);
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
}