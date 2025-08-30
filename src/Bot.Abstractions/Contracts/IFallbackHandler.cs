namespace Bot.Abstractions.Contracts;

/// <summary>
///     Обработчик, вызываемый когда не найден другой обработчик
/// </summary>
public interface IFallbackHandler : IUpdateHandler
{
}
