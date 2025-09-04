using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Scenes;

namespace Bot.Examples.HelloBot.Scenes;

/// <summary>
///     Сцена заполнения профиля.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запрашивает имя и возраст.</item>
///     </list>
/// </remarks>
public sealed class ProfileScene : IScene
{
    private readonly IScene _scene;

    /// <summary>
    ///     Создаёт сцену.
    /// </summary>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="client">Клиент транспорта.</param>
    /// <param name="ttl">Время ожидания шага.</param>
    public ProfileScene(ISceneNavigator navigator, ITransportClient client, TimeSpan ttl)
    {
        _scene = new WizardSceneBuilder()
            .AddStep("как вас зовут?", Validators.NotEmpty, "имя не должно быть пустым", ttl)
            .AddStep("сколько вам лет?", Validators.IsInt, "укажите возраст числом", ttl)
            .OnFinish(async (ctx, data) =>
            {
                var name = data[0];
                var age = data[1];
                await client.SendTextAsync(ctx.Chat, $"имя: {name}, возраст: {age}", ctx.CancellationToken);
            })
            .Build("profile", navigator, client);
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
