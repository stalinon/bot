using System.Text.Json;

using FluentAssertions;

using Stalinon.Bot.Scheduler;

using Xunit;

namespace Stalinon.Bot.Scheduler.Tests;

/// <summary>
///     Тесты <see cref="JobDescriptor" />: сравнение и сериализация.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется сравнение по значению</item>
///         <item>Проверяется сериализация в JSON и обратно</item>
///     </list>
/// </remarks>
public sealed class JobDescriptorTests
{
    /// <inheritdoc />
    public JobDescriptorTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен сравниваться по значению.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен сравниваться по значению")]
    public void Should_CompareByValue()
    {
        // Arrange
        var left = new JobDescriptor(typeof(int), "* * * * *", TimeSpan.FromSeconds(1));
        var right = new JobDescriptor(typeof(int), "* * * * *", TimeSpan.FromSeconds(1));

        // Act & Assert
        left.Should().Be(right);
    }

    /// <summary>
    ///     Тест 2: Должен сериализоваться и десериализоваться без потерь.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен сериализоваться и десериализоваться без потерь")]
    public void Should_SerializeAndDeserialize()
    {
        // Arrange
        var descriptor = new JobDescriptor(typeof(string), null, TimeSpan.FromSeconds(2));

        // Act
        var json = JsonSerializer.Serialize(descriptor, Json);
        var restored = JsonSerializer.Deserialize<JobDescriptor>(json, Json);

        // Assert
        restored.Should().Be(descriptor);
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new TypeConverter() }
    };

    private sealed class TypeConverter : System.Text.Json.Serialization.JsonConverter<Type>
    {
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Type.GetType(reader.GetString()!)!;
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.AssemblyQualifiedName);
        }
    }
}
