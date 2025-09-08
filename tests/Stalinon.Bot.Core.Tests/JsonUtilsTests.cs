using FluentAssertions;

using Stalinon.Bot.Core.Utils;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
/// Тесты JSON утилит.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Проверяется сериализация</item>
/// <item>Проверяется десериализация</item>
/// </list>
/// </remarks>
public sealed class JsonUtilsTests
{
    /// <summary>
    /// Тест 1: Должен сериализовать объект в JSON.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен сериализовать объект в JSON")]
    public void Should_SerializeObject()
    {
        var obj = new TestDto(1, "x");

        var json = JsonUtils.Serialize(obj);

        json.Should().Be("{\"A\":1,\"B\":\"x\"}");
    }

    /// <summary>
    /// Тест 2: Должен десериализовать JSON в объект.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен десериализовать JSON в объект")]
    public void Should_DeserializeObject()
    {
        const string json = "{\"A\":1,\"B\":\"x\"}";

        var obj = JsonUtils.Deserialize<TestDto>(json);

        obj!.A.Should().Be(1);
        obj.B.Should().Be("x");
    }

    private sealed record TestDto(int A, string B);
}

