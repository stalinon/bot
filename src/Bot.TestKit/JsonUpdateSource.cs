using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;

namespace Bot.TestKit;

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
                Transport: u.Transport ?? "test",
                UpdateId: u.UpdateId ?? string.Empty,
                Chat: u.Chat ?? new ChatAddress(0),
                User: u.User ?? new UserAddress(0),
                Text: u.Text,
                Command: u.Command,
                Args: u.Args,
                Payload: u.Payload,
                Items: u.Items ?? new Dictionary<string, object>(),
                Services: null!,
                CancellationToken: default);
            await onUpdate(ctx);
        }
    }

    private sealed class JsonUpdate
    {
        public string? Transport { get; set; }
        public string? UpdateId { get; set; }
        public ChatAddress? Chat { get; set; }
        public UserAddress? User { get; set; }
        public string? Text { get; set; }
        public string? Command { get; set; }
        public string[]? Args { get; set; }
        public string? Payload { get; set; }
        public Dictionary<string, object>? Items { get; set; }
    }
}
