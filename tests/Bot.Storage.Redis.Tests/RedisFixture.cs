using System.Diagnostics;
using StackExchange.Redis;
using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Фикстура, поднимающая сервер Redis.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private Process? _process;

    /// <summary>
    ///     Подключение к Redis.
    /// </summary>
    public IConnectionMultiplexer Connection { get; private set; } = null!;

    /// <summary>
    ///     Инициализация Redis перед тестами.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            Connection = await ConnectionMultiplexer.ConnectAsync("localhost");
        }
        catch
        {
            _process = Process.Start("redis-server", "--save '' --appendonly no");
            await Task.Delay(1000);
            Connection = await ConnectionMultiplexer.ConnectAsync("localhost");
        }
    }

    /// <summary>
    ///     Завершение работы Redis после тестов.
    /// </summary>
    public async Task DisposeAsync()
    {
        Connection.Dispose();
        if (_process is { HasExited: false })
        {
            _process.Kill();
            await _process.WaitForExitAsync();
        }
    }
}
