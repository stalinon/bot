using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Middlewares;

/// <summary>
///     Парсинг команд
/// </summary>
public sealed class CommandParsingMiddleware : IUpdateMiddleware
{
    /// <inheritdoc />
    public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var text = ctx.Text;
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
        {
            return next(ctx);
        }

        var space = text.IndexOf(' ');
        var cmd = space < 0 ? text : text[..space];
        var payload = space < 0 ? null : text[(space + 1)..].Trim();
        ctx = ctx with
        {
            Command = cmd, Payload = payload
        };
        
        return next(ctx);
    }
}