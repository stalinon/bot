using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Tests;

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
    public Task<bool> CanEnter(UpdateContext ctx) => Task.FromResult(true);

    /// <inheritdoc />
    public Task OnEnter(UpdateContext ctx) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnUpdate(UpdateContext ctx) => Task.CompletedTask;
}
