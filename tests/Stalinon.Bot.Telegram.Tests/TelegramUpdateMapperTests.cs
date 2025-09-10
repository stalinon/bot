using System.Reflection;

using FluentAssertions;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Xunit;

namespace Stalinon.Bot.Telegram.Tests;

/// <summary>
///     Тесты преобразования обновлений Telegram в контекст.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Преобразование текстового сообщения.</item>
///         <item>Преобразование сообщения с WebApp данными.</item>
///         <item>Преобразование callback query.</item>
///         <item>Возврат null для неподдерживаемого обновления.</item>
///     </list>
/// </remarks>
public sealed class TelegramUpdateMapperTests
{
    /// <inheritdoc />
    public TelegramUpdateMapperTests()
    {
    }

    /// <summary>
    ///     Тест 1: Преобразует текстовое сообщение в контекст.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Преобразует текстовое сообщение в контекст.")]
    public void Should_MapTextMessage()
    {
        // Arrange
        var update = new Update
        {
            Id = 1,
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, Username = "u", LanguageCode = "ru" },
                Text = "t"
            }
        };

        // Act
        var ctx = Map(update);

        // Assert
        ctx.Should().NotBeNull();
        ctx!.Transport.Should().Be("telegram");
        ctx.Chat.Should().BeEquivalentTo(new ChatAddress(10, ChatType.Private.ToString()));
        ctx.User.Should().BeEquivalentTo(new UserAddress(20, "u", "ru"));
        ctx.Text.Should().Be("t");
        ctx.Payload.Should().BeNull();
        ctx.Items.Should().ContainKey(UpdateItems.UpdateType).WhoseValue.Should().Be("Message");
        ctx.Items.Should().ContainKey(UpdateItems.MessageId).WhoseValue.Should().Be(0);
        ctx.Items.Should().NotContainKey(UpdateItems.WebAppData);
    }

    /// <summary>
    ///     Тест 2: Преобразует сообщение с WebApp данными в контекст.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Преобразует сообщение с WebApp данными в контекст.")]
    public void Should_MapMessageWithWebAppData()
    {
        // Arrange
        var update = new Update
        {
            Id = 1,
            Message = new Message
            {
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 10, Type = ChatType.Private },
                From = new User { Id = 20, Username = "u", LanguageCode = "ru" },
                Text = "t",
                WebAppData = new WebAppData { Data = "p" }
            }
        };

        // Act
        var ctx = Map(update);

        // Assert
        ctx.Should().NotBeNull();
        ctx!.Payload.Should().Be("p");
        ctx.Items.Should().ContainKey(UpdateItems.WebAppData).WhoseValue.Should().Be(true);
    }

    /// <summary>
    ///     Тест 3: Преобразует callback query в контекст.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Преобразует callback query в контекст.")]
    public void Should_MapCallbackQuery()
    {
        // Arrange
        var update = new Update
        {
            Id = 1,
            CallbackQuery = new CallbackQuery
            {
                Id = "1",
                Data = "d",
                From = new User { Id = 20, Username = "u", LanguageCode = "ru" },
                Message = new Message
                {
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 10, Type = ChatType.Private }
                }
            }
        };

        // Act
        var ctx = Map(update);

        // Assert
        ctx.Should().NotBeNull();
        ctx!.Payload.Should().Be("d");
        ctx.Chat.Should().BeEquivalentTo(new ChatAddress(10, ChatType.Private.ToString()));
        ctx.User.Should().BeEquivalentTo(new UserAddress(20, "u", "ru"));
        ctx.Items.Should().ContainKey(UpdateItems.MessageId).WhoseValue.Should().Be(0);
    }

    /// <summary>
    ///     Тест 4: Возвращает null для неподдерживаемого обновления.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Возвращает null для неподдерживаемого обновления.")]
    public void Should_ReturnNull_OnUnsupportedUpdate()
    {
        // Arrange
        var update = new Update { Id = 1 };

        // Act
        var ctx = Map(update);

        // Assert
        ctx.Should().BeNull();
    }

    private static UpdateContext? Map(Update u)
    {
        var type = typeof(WebhookService).Assembly.GetType("Stalinon.Bot.Telegram.TelegramUpdateMapper")!;
        var method = type.GetMethod("Map", BindingFlags.Static | BindingFlags.Public)!;
        return (UpdateContext?)method.Invoke(null, new object?[] { u });
    }
}

