using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Routing;

/// <summary>
///     Регистр обработчиков
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Сопоставляет команды обработчикам</item>
///         <item>Автоматически привязывает аргументы к типам</item>
///     </list>
/// </remarks>
public sealed class HandlerRegistry
{
    private readonly List<(string[] Path, Type Handler, Type? ArgsType)> _commands = [];
    private readonly List<(Regex Pattern, Type Type)> _regexHandlers = [];

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
            var path = ca.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _commands.Add((path, t, ca.ArgsType));
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
        if (!string.IsNullOrEmpty(ctx.Command))
        {
            var tokens = new[] { ctx.Command! }.Concat(ctx.Args ?? []).ToArray();

            foreach (var (path, handler, argsType) in _commands.OrderByDescending(c => c.Path.Length))
            {
                if (tokens.Length < path.Length)
                {
                    continue;
                }

                var match = true;
                for (var i = 0; i < path.Length; i++)
                {
                    if (!string.Equals(tokens[i], path[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = false;
                        break;
                    }
                }

                if (match && MatchesFilter(handler, ctx))
                {
                    if (argsType is not null)
                    {
                        var argsTokens = tokens[path.Length..];
                        var bound = BindArgs(argsType, argsTokens);
                        if (bound is null)
                        {
                            return null;
                        }

                        ctx.SetItem(UpdateItems.CommandArgs, bound);
                    }

                    return handler;
                }
            }
        }

        if (!string.IsNullOrEmpty(ctx.Text))
        {
            foreach (var (p, type) in _regexHandlers)
            {
                if (p.IsMatch(ctx.Text!) && MatchesFilter(type, ctx))
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static object? BindArgs(Type t, string[] tokens)
    {
        var ctor = t.GetConstructors().SingleOrDefault();
        if (ctor is null)
        {
            return null;
        }

        var parameters = ctor.GetParameters();
        if (parameters.Length != tokens.Length)
        {
            return null;
        }

        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            try
            {
                values[i] = Convert.ChangeType(tokens[i], parameters[i].ParameterType);
            }
            catch
            {
                return null;
            }
        }

        var instance = ctor.Invoke(values);

        try
        {
            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }
        catch (ValidationException)
        {
            return null;
        }

        return instance;
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
