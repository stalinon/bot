using System.Threading.Tasks;

namespace Bot.Abstractions.Contracts;

/// <summary>
///     Функция обновления
/// </summary>
public delegate ValueTask UpdateDelegate(UpdateContext ctx);
