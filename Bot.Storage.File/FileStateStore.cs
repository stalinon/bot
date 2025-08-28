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
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    
    ///  <inheritdoc cref="FileStateStore"/>
    public FileStateStore(FileStoreOptions options)
    {
        _basePath = options.Path;
        Directory.CreateDirectory(_basePath);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
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
        var dir = DirFor(scope);
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"{San(key)}.tmp");
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
        var file = PathFor(scope, key);
        if (!System.IO.File.Exists(file))
        {
            return Task.FromResult(false);
        }

        System.IO.File.Delete(file);
        return Task.FromResult(true);
    }
    
    private string DirFor(string scope) => Path.Combine(_basePath, San(scope));
    private string PathFor(string scope, string key) => Path.Combine(DirFor(scope), $"{San(key)}.json");
    private static string San(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
}