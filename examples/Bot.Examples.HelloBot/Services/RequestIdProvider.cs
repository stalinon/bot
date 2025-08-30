namespace Bot.Examples.HelloBot.Services;

/// <summary>
///     Provides a unique request identifier for each update.
/// </summary>
public sealed class RequestIdProvider
{
    /// <summary>
    ///     Gets the identifier for the current request scope.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
