using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тестовая сцена.
/// </summary>
public sealed class DummyScene : IScene
{
    /// <summary>
    ///     Создаёт тестовую сцену.
    /// </summary>
    /// <param name="name">Название сцены.</param>
    public DummyScene(string name = "test")
    {
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<bool> CanEnter(UpdateContext ctx)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task OnEnter(UpdateContext ctx)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnUpdate(UpdateContext ctx)
    {
        return Task.CompletedTask;
    }
}
