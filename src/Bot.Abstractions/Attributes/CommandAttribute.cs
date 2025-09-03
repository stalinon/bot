namespace Bot.Abstractions.Attributes;

/// <summary>
///     Атрибут-метка команды
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CommandAttribute(string name) : Attribute
{
    /// <summary>
    ///     Название
    /// </summary>
    public string Name { get; } = name;
}
