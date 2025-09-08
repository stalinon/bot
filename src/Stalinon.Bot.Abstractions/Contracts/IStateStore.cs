namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Хранилище состояний.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Обеспечивает доступ к состояниям пользователей</item>
///         <item>Поддерживает TTL и атомарные операции</item>
///     </list>
/// </remarks>
public interface IStateStore : IStateStorage
{
}
