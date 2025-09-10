using FluentAssertions;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;

using Xunit;

namespace Stalinon.Bot.Abstractions.Tests;

/// <summary>
///     Тесты <see cref="UpdateContext"/> и <see cref="UpdateItems"/>: проверка хранения и извлечения элементов
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Возврат установленного элемента</item>
///         <item>Возврат значения по предопределённым ключам</item>
///         <item>Отсутствие значения при неизвестном ключе</item>
///     </list>
/// </remarks>
public sealed class UpdateContextTests
{
    /// <inheritdoc/>
    public UpdateContextTests()
    {
    }

    private static UpdateContext CreateContext()
    {
        return new(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            null!,
            CancellationToken.None);
    }

    /// <summary>
    ///     Тест 1: Должен возвращать ранее установленное значение по ключу
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен возвращать ранее установленное значение по ключу")]
    public void Should_ReturnValue_When_ItemWasSet()
    {
        var ctx = CreateContext();
        ctx.SetItem("key", 42);

        ctx.GetItem<int>("key").Should().Be(42);
    }

    /// <summary>
    ///     Тест 2: Должен возвращать значение по предопределённому ключу UpdateType
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен возвращать значение по предопределённому ключу UpdateType")]
    public void Should_GetSetItem_ForUpdateType()
    {
        var ctx = CreateContext();
        ctx.SetItem(UpdateItems.UpdateType, "message");

        ctx.GetItem<string>(UpdateItems.UpdateType).Should().Be("message");
    }

    /// <summary>
    ///     Тест 3: Должен возвращать значение по предопределённому ключу MessageId
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен возвращать значение по предопределённому ключу MessageId")]
    public void Should_GetSetItem_ForMessageId()
    {
        var ctx = CreateContext();
        ctx.SetItem(UpdateItems.MessageId, 10);

        ctx.GetItem<int>(UpdateItems.MessageId).Should().Be(10);
    }

    /// <summary>
    ///     Тест 4: Должен возвращать значение по предопределённому ключу Handler
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен возвращать значение по предопределённому ключу Handler")]
    public void Should_GetSetItem_ForHandler()
    {
        var ctx = CreateContext();
        ctx.SetItem(UpdateItems.Handler, "demo");

        ctx.GetItem<string>(UpdateItems.Handler).Should().Be("demo");
    }

    /// <summary>
    ///     Тест 5: Должен возвращать значение по предопределённому ключу WebAppData
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен возвращать значение по предопределённому ключу WebAppData")]
    public void Should_GetSetItem_ForWebAppData()
    {
        var ctx = CreateContext();
        ctx.SetItem(UpdateItems.WebAppData, true);

        ctx.GetItem<bool>(UpdateItems.WebAppData).Should().BeTrue();
    }

    /// <summary>
    ///     Тест 6: Должен возвращать значение по предопределённому ключу TraceContext
    /// </summary>
    [Fact(DisplayName = "Тест 6: Должен возвращать значение по предопределённому ключу TraceContext")]
    public void Should_GetSetItem_ForTraceContext()
    {
        var ctx = CreateContext();
        var trace = new object();
        ctx.SetItem(UpdateItems.TraceContext, trace);

        ctx.GetItem<object>(UpdateItems.TraceContext).Should().Be(trace);
    }

    /// <summary>
    ///     Тест 7: Должен возвращать значение по предопределённому ключу CommandArgs
    /// </summary>
    [Fact(DisplayName = "Тест 7: Должен возвращать значение по предопределённому ключу CommandArgs")]
    public void Should_GetSetItem_ForCommandArgs()
    {
        var ctx = CreateContext();
        var args = new[] { "one", "two" };
        ctx.SetItem(UpdateItems.CommandArgs, args);

        ctx.GetItem<string[]>(UpdateItems.CommandArgs).Should().BeEquivalentTo(args);
    }

    /// <summary>
    ///     Тест 8: Должен возвращать null при отсутствии ключа
    /// </summary>
    [Fact(DisplayName = "Тест 8: Должен возвращать null при отсутствии ключа")]
    public void Should_ReturnNull_When_KeyMissing()
    {
        var ctx = CreateContext();

        ctx.GetItem<string>("missing").Should().BeNull();
    }
}
