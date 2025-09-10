using System.Linq;

using FluentAssertions;

using Stalinon.Bot.Core.Transport;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты GuidMessageKeyProvider: проверка уникальности и формата ключей.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется формат выдаваемых ключей.</item>
///         <item>Проверяется уникальность ключей.</item>
///     </list>
/// </remarks>
public sealed class GuidMessageKeyProviderTests
{
    /// <summary>
    ///     Тест 1: Должен возвращать ключ в формате N из 32 шестнадцатеричных символов.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен возвращать ключ в формате N из 32 шестнадцатеричных символов")]
    public void Should_ReturnKeyInNFormat()
    {
        var provider = new GuidMessageKeyProvider();

        var key = provider.Next();

        key.Length.Should().Be(32);
        key.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    /// <summary>
    ///     Тест 2: Должен генерировать уникальные ключи.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен генерировать уникальные ключи")]
    public void Should_GenerateUniqueKeys()
    {
        var provider = new GuidMessageKeyProvider();

        var keys = Enumerable.Range(0, 1000).Select(_ => provider.Next()).ToList();

        keys.Should().OnlyHaveUniqueItems();
    }
}
