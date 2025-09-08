using System.Text.Json;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.TestKit;

/// <summary>
///     Источник обновлений, читающий их из JSON-файла.
/// </summary>
public sealed class JsonUpdateSource(string path) : IUpdateSource
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        var updates = await JsonSerializer.DeserializeAsync<List<JsonUpdate>>(fs, Json, ct);
        if (updates is null)
        {
            return;
        }

        foreach (var u in updates)
        {
            ct.ThrowIfCancellationRequested();
            var ctx = new UpdateContext(
                u.Transport ?? "test",
                u.UpdateId ?? string.Empty,
                u.Chat ?? new ChatAddress(0),
                u.User ?? new UserAddress(0),
                u.Text,
                u.Command,
                u.Args,
                u.Payload,
                u.Items ?? new Dictionary<string, object>(),
                null!,
                default);
            await onUpdate(ctx);
        }
    }

    /// <summary>
    ///     Остановить источник.
    /// </summary>
    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    private sealed class JsonUpdate
    {
        public string? Transport { get; init; }
        public string? UpdateId { get; init; }
        public ChatAddress? Chat { get; init; }
        public UserAddress? User { get; init; }
        public string? Text { get; init; }
        public string? Command { get; init; }
        public string[]? Args { get; init; }
        public string? Payload { get; init; }
        public Dictionary<string, object>? Items { get; init; }
    }
}
