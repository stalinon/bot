using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Scenes;

using Microsoft.Extensions.DependencyInjection;

namespace Bot.TestKit;

/// <summary>
///     Вспомогательные методы для тестирования сцен.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Создаёт контексты обновлений</item>
///         <item>Позволяет прогонять шаги с учётом TTL</item>
///     </list>
/// </remarks>
public static class SceneTestExtensions
{
    /// <summary>
    ///     Создать контекст обновления.
    /// </summary>
    public static UpdateContext CreateContext(string text, string? command = null)
    {
        return new UpdateContext(
            "tg",
            Guid.NewGuid().ToString(),
            new ChatAddress(1),
            new UserAddress(1),
            text,
            command,
            null,
            null,
            new Dictionary<string, object>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }

    /// <summary>
    ///     Выполнить шаг сцены и проверить TTL.
    /// </summary>
    public static async Task<SceneState?> StepAsync(
        this IUpdateHandler handler,
        SceneNavigator navigator,
        UpdateContext ctx,
        string text,
        string? command = null,
        TimeSpan? delay = null)
    {
        if (delay.HasValue)
        {
            await Task.Delay(delay.Value);
        }

        var next = ctx with { Text = text, Command = command };
        await handler.HandleAsync(next);
        return await navigator.GetStateAsync(next);
    }
}
