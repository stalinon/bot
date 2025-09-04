using System.Text.RegularExpressions;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Examples.HelloBot.Scenes;

/// <summary>
///     Сцена ввода номера телефона с подтверждением.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запрашивает номер у пользователя.</item>
///         <item>Проверяет корректность и ждёт подтверждения.</item>
///     </list>
/// </remarks>
public sealed class PhoneScene : IScene
{
    private readonly ITransportClient _client;
    private readonly ISceneNavigator _navigator;
    private readonly TimeSpan _ttl;

    /// <summary>
    ///     Создаёт сцену ввода телефона.
    /// </summary>
    /// <param name="client">Клиент транспорта.</param>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="ttl">Время ожидания шага.</param>
    public PhoneScene(ITransportClient client, ISceneNavigator navigator, TimeSpan ttl)
    {
        _client = client;
        _navigator = navigator;
        _ttl = ttl;
    }

    /// <inheritdoc />
    public string Name => "phone";

    /// <inheritdoc />
    public Task<bool> CanEnter(UpdateContext ctx)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task OnEnter(UpdateContext ctx)
    {
        await _client.SendTextAsync(ctx.Chat, "введите номер телефона", ctx.CancellationToken);
        await _navigator.NextStepAsync(ctx, ttl: _ttl);
    }

    /// <inheritdoc />
    public async Task OnUpdate(UpdateContext ctx)
    {
        var state = await _navigator.GetStateAsync(ctx);
        if (state is null || state.Scene != Name)
        {
            return;
        }

        if (state.Step == 1)
        {
            if (ctx.Text is null)
            {
                return;
            }

            if (Regex.IsMatch(ctx.Text, @"^\+?\d{10,15}$"))
            {
                await _client.SendTextAsync(ctx.Chat, $"подтвердите номер: {ctx.Text} (да/нет)", ctx.CancellationToken);
                await _navigator.NextStepAsync(ctx, ctx.Text, _ttl);
            }
            else
            {
                await _client.SendTextAsync(ctx.Chat, "номер некорректен, попробуйте ещё раз", ctx.CancellationToken);
            }

            return;
        }

        if (state.Step == 2)
        {
            if (ctx.Text is null)
            {
                return;
            }

            var answer = ctx.Text.Trim().ToLowerInvariant();
            if (answer == "да")
            {
                await _client.SendTextAsync(ctx.Chat, $"номер сохранён: {state.Data}", ctx.CancellationToken);
                await _navigator.ExitAsync(ctx);
            }
            else if (answer == "нет")
            {
                await _navigator.EnterAsync(ctx, this);
            }
            else
            {
                await _client.SendTextAsync(ctx.Chat, "ответьте \"да\" или \"нет\"", ctx.CancellationToken);
            }
        }
    }
}
