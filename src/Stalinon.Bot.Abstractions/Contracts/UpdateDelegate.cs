using System.Threading.Tasks;

namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Функция обновления
/// </summary>
public delegate ValueTask UpdateDelegate(UpdateContext ctx);
