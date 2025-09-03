using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Scenes;
using Bot.TestKit;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты вспомогательных методов навигатора.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется повторный ввод.</item>
///     </list>
/// </remarks>
public sealed class SceneNavigatorExtensionsTests
{
    /// <summary>
    ///     Тест 1. Проверяем повторный ввод после ошибки.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем повторный ввод после ошибки")]
    public async Task RetryOnInvalidInput()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMinutes(1));
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);

        await navigator.EnterAsync(ctx, new DummyScene("ask"));

        await navigator.AskTextAsync(ctx, "Введите число", Validators.IsInt, "Ошибка");
        var wrongCtx = ctx with { Text = "abc" };
        var first = await navigator.AskTextAsync(wrongCtx, "Введите число", Validators.IsInt, "Ошибка");
        Assert.Null(first);
        Assert.Equal("Ошибка", client.SentTexts[^1].text);

        var okCtx = ctx with { Text = "123" };
        var result = await navigator.AskTextAsync(okCtx, "Введите число", Validators.IsInt, "Ошибка");
        Assert.Equal("123", result);
    }
}
