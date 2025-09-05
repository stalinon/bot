using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Middlewares;

/// <summary>
///     Парсинг команд
/// </summary>
public sealed class CommandParsingMiddleware : IUpdateMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var result = CommandParser.Parse(ctx.Text);
        if (result is null)
        {
            return next(ctx);
        }

        ctx = ctx with
        {
            Command = result.Command,
            Payload = result.Payload,
            Args = result.Args
        };

        return next(ctx);
    }
}
