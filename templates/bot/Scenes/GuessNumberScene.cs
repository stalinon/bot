using System;
using System.Threading.Tasks;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Scenes;

namespace BotApp.Scenes;

/// <summary>
///	Сцена угадай число.
/// </summary>
/// <remarks>
///	<list type="number">
///		<item>Предлагает угадать число от 1 до 3.</item>
///	</list>
/// </remarks>
public sealed class GuessNumberScene : IScene
{
    private readonly IScene _scene;

    /// <summary>
    ///	Создаёт сцену.
    /// </summary>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="client">Клиент транспорта.</param>
    public GuessNumberScene(ISceneNavigator navigator, ITransportClient client)
    {
        _scene = new WizardSceneBuilder()
            .AddStep(
                "угадай число от 1 до 3",
                text => int.TryParse(text, out var n) && n is >= 1 and <= 3,
                "введи число от 1 до 3",
                TimeSpan.FromMinutes(1))
            .OnFinish(async (ctx, data) =>
            {
                var guess = int.Parse(data[0]);
                var secret = Random.Shared.Next(1, 4);
                var reply = guess == secret ? "угадал" : $"не угадал, было {secret}";
                await client.SendTextAsync(ctx.Chat, reply, ctx.CancellationToken);
            })
            .Build("guess", navigator, client);
    }

    /// <inheritdoc />
    public string Name => _scene.Name;

    /// <inheritdoc />
    public Task<bool> CanEnter(UpdateContext ctx)
    {
        return _scene.CanEnter(ctx);
    }

    /// <inheritdoc />
    public Task OnEnter(UpdateContext ctx)
    {
        return _scene.OnEnter(ctx);
    }

    /// <inheritdoc />
    public Task OnUpdate(UpdateContext ctx)
    {
        return _scene.OnUpdate(ctx);
    }
}

