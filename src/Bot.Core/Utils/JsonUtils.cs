using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Bot.Core.Utils;

/// <summary>
/// Утилиты JSON сериализации и парсинга.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Использует пул массивов для уменьшения аллокаций</item>
/// <item>Избегает лишних строковых копий</item>
/// </list>
/// </remarks>
public static class JsonUtils
{
    /// <summary>
    /// Сериализует объект в JSON.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="value">Экземпляр для сериализации.</param>
    /// <returns>Строка JSON.</returns>
    public static string Serialize<T>(T value)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            using var stream = new MemoryStream(buffer);
            JsonSerializer.Serialize(stream, value);
            return Encoding.UTF8.GetString(buffer, 0, (int)stream.Position);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Десериализует объект из JSON.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="json">JSON-строка.</param>
    /// <returns>Экземпляр объекта или <c>null</c>.</returns>
    public static T? Deserialize<T>(string json)
    {
        var byteCount = Encoding.UTF8.GetByteCount(json);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
            return JsonSerializer.Deserialize<T>(buffer.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
