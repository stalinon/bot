using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Проба транспорта.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Выполняет лёгкую операцию транспортного клиента.</item>
///     </list>
/// </remarks>
internal sealed class TransportProbe(ITransportClient client) : IHealthProbe
{
    private static readonly ChatAddress ProbeChat = new(0);

    /// <summary>
    ///     Проверить транспорт.
    /// </summary>
    public async Task ProbeAsync(CancellationToken ct)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        await client.SendChatActionAsync(ProbeChat, ChatAction.Typing, linked.Token);
    }
}
