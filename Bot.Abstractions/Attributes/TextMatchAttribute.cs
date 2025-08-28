using System.Text.RegularExpressions;

namespace Bot.Abstractions.Attributes;

/// <summary>
///     Атрибут-метка команды с реджексом
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TextMatchAttribute(string regex) : Attribute
{
    /// <summary>
    ///     Реджекс
    /// </summary>
    public Regex Pattern { get; } = new(regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
}