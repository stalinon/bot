using System.Text.Json;

using FluentAssertions;

using StackExchange.Redis;

using Xunit;

namespace Stalinon.Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты RedisOptions: проверка значений по умолчанию и обработки недопустимых параметров.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется установка значений по умолчанию</item>
///         <item>Проверяется выброс исключения при отсутствии подключения</item>
///     </list>
/// </remarks>
public sealed class RedisOptionsTests
{
    /// <inheritdoc />
    public RedisOptionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен заполнять значения по умолчанию.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен заполнять значения по умолчанию.")]
    public async Task Should_SetDefaults_When_NotSpecified()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux };

        // Act & Assert
        options.Database.Should().Be(0);
        options.Prefix.Should().BeEmpty();
        options.Serialization.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        mux.Dispose();
    }

    /// <summary>
    ///     Тест 2: Должен выбрасывать исключение при отсутствии подключения.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выбрасывать исключение при отсутствии подключения.")]
    public void Should_Throw_When_ConnectionMissing()
    {
        // Arrange
        var options = new RedisOptions();

        // Act
        var act = () => new RedisStateStore(options);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }
}

