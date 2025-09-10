using FluentAssertions;

using Stalinon.Bot.Abstractions.Addresses;

using Xunit;

namespace Stalinon.Bot.Abstractions.Tests;

/// <summary>
///     Тесты адресов: сравнение значений <see cref="ChatAddress"/> и <see cref="UserAddress"/>
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Равенство по значениям</item>
///         <item>Неравенство при различиях</item>
///     </list>
/// </remarks>
public sealed class AddressTests
{
    /// <inheritdoc/>
    public AddressTests()
    {
    }

    /// <summary>
    ///     Тест 1: Чаты с одинаковыми данными равны
    /// </summary>
    [Fact(DisplayName = "Тест 1: Чаты с одинаковыми данными равны")]
    public void Should_BeEqual_ForChatAddress_WithSameData()
    {
        var a = new ChatAddress(1, "private");
        var b = new ChatAddress(1, "private");

        a.Should().Be(b);
    }

    /// <summary>
    ///     Тест 2: Чаты с разными данными не равны
    /// </summary>
    [Fact(DisplayName = "Тест 2: Чаты с разными данными не равны")]
    public void Should_NotBeEqual_ForChatAddress_WithDifferentData()
    {
        var a = new ChatAddress(1, "private");
        var b = new ChatAddress(2, "group");

        a.Should().NotBe(b);
    }

    /// <summary>
    ///     Тест 3: Пользователи с одинаковыми данными равны
    /// </summary>
    [Fact(DisplayName = "Тест 3: Пользователи с одинаковыми данными равны")]
    public void Should_BeEqual_ForUserAddress_WithSameData()
    {
        var a = new UserAddress(1, "name", "ru");
        var b = new UserAddress(1, "name", "ru");

        a.Should().Be(b);
    }

    /// <summary>
    ///     Тест 4: Пользователи с разными данными не равны
    /// </summary>
    [Fact(DisplayName = "Тест 4: Пользователи с разными данными не равны")]
    public void Should_NotBeEqual_ForUserAddress_WithDifferentData()
    {
        var a = new UserAddress(1, "name", "ru");
        var b = new UserAddress(2, "other", "en");

        a.Should().NotBe(b);
    }
}
