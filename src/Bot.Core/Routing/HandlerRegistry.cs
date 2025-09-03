using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Routing;

/// <summary>
///     Регистр обработчиков
/// </summary>
public sealed class HandlerRegistry
{
    private readonly ConcurrentDictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(System.Text.RegularExpressions.Regex Pattern, Type Type)> _regexHandlers = new();

    /// <summary>
    ///     Зарегистрировать обработчики из сборки
    /// </summary>
    public void RegisterFrom(Assembly assembly)
    {
        foreach (var t in assembly.GetTypes().OrderBy(t => t.MetadataToken))
        {
            Register(t);
        }
    }

    /// <summary>
    ///     Зарегистрировать обработчик
    /// </summary>
    public void Register(Type t)
    {
        if (!typeof(IUpdateHandler).IsAssignableFrom(t) || t.IsAbstract || t.IsInterface)
        {
            return;
        }

        foreach (var ca in t.GetCustomAttributes<CommandAttribute>())
        {
            _commands[ca.Name] = t;
        }

        foreach (var ta in t.GetCustomAttributes<TextMatchAttribute>())
        {
            _regexHandlers.Add((ta.Pattern, t));
        }
    }

    /// <summary>
    ///     Отыскать обработчик для команды
    /// </summary>
    public Type? FindFor(UpdateContext ctx)
    {
        if (!string.IsNullOrEmpty(ctx.Command) && _commands.TryGetValue(ctx.Command!, out var t))
        {
            if (MatchesFilter(t, ctx)) return t;
        }

        if (!string.IsNullOrEmpty(ctx.Text))
        {
            foreach (var (p, type) in _regexHandlers)
            {
                if (p.IsMatch(ctx.Text!) && MatchesFilter(type, ctx)) return type;
            }
        }

        return null;
    }

    private static bool MatchesFilter(Type t, UpdateContext ctx)
    {
        var f = t.GetCustomAttribute<UpdateFilterAttribute>();
        if (f is null)
        {
            return true;
        }

        if (f.WebAppData && ctx.GetItem<bool>(UpdateItems.WebAppData) != true)
        {
            return false;
        }

        return true;
    }
}
