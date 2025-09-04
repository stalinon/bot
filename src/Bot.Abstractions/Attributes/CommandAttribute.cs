using System;

namespace Bot.Abstractions.Attributes;

/// <summary>
///     Атрибут-метка команды
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    ///     Инициализирует атрибут команды.
    /// </summary>
    /// <param name="name">Имя команды без слеша, допускающее подкоманды через пробел.</param>
    /// <param name="argsType">Тип аргументов команды.</param>
    public CommandAttribute(string name, Type? argsType = null)
    {
        Name = name;
        ArgsType = argsType;
    }

    /// <summary>
    ///     Имя команды.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Тип аргументов.
    /// </summary>
    public Type? ArgsType { get; }
}
