using System.Collections.Concurrent;
using System.Reflection;
using System.Linq;
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
            return t;
        }

        if (!string.IsNullOrEmpty(ctx.Text))
        {
            foreach (var (p, type) in _regexHandlers)
            {
                if (p.IsMatch(ctx.Text!)) return type;
            }
        }
        
        return null;
    }
}