using System.Text.RegularExpressions;

using FluentAssertions;

using Stalinon.Bot.Abstractions.Attributes;

using Xunit;

namespace Stalinon.Bot.Abstractions.Tests;

/// <summary>
///     Тесты атрибутов маршрутизации и фильтрации
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Имя и тип аргументов команды</item>
///         <item>Компиляция и совпадение регулярного выражения</item>
///         <item>Признак данных веб-приложения</item>
///     </list>
/// </remarks>
public sealed class AttributeTests
{
    /// <inheritdoc/>
    public AttributeTests()
    {
    }

    /// <summary>
    ///     Тест 1: Атрибут команды хранит имя и тип аргументов
    /// </summary>
    [Fact(DisplayName = "Тест 1: Атрибут команды хранит имя и тип аргументов")]
    public void Should_StoreNameAndArgsType_InCommandAttribute()
    {
        var attr = new CommandAttribute("start", typeof(string));

        attr.Name.Should().Be("start");
        attr.ArgsType.Should().Be(typeof(string));
    }

    /// <summary>
    ///     Тест 2: Атрибут текстового соответствия компилирует шаблон
    /// </summary>
    [Fact(DisplayName = "Тест 2: Атрибут текстового соответствия компилирует шаблон")]
    public void Should_CompilePattern_InTextMatchAttribute()
    {
        var attr = new TextMatchAttribute("^a+$");

        attr.Pattern.IsMatch("aaa").Should().BeTrue();
        attr.Pattern.Options.Should().Be(
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }

    /// <summary>
    ///     Тест 3: Атрибут фильтра обновлений задаёт признак данных веб-приложения
    /// </summary>
    [Fact(DisplayName = "Тест 3: Атрибут фильтра обновлений задаёт признак данных веб-приложения")]
    public void Should_SetWebAppData_InUpdateFilterAttribute()
    {
        var attr = new UpdateFilterAttribute { WebAppData = true };

        attr.WebAppData.Should().BeTrue();
        new UpdateFilterAttribute().WebAppData.Should().BeFalse();
    }
}
