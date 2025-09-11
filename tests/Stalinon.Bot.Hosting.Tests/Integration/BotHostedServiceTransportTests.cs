using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Pipeline;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Core.Utils;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests;

/// <summary>
///     Интеграционные тесты <see cref="BotHostedService" /> в разных режимах транспорта.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется запуск с поллингом.</item>
///         <item>Проверяется запуск с вебхуком.</item>
///     </list>
/// </remarks>
public sealed class BotHostedServiceTransportTests
{
    /// <inheritdoc />
    public BotHostedServiceTransportTests()
    {
    }

    /// <summary>
    ///     Тест 1: Сервис запускается при поллинге.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Сервис запускается при поллинге")]
    public async Task Should_StartWithPolling()
    {
        // Arrange
        var svc = CreateService(TransportMode.Polling);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var act = async () =>
        {
            await svc.StartAsync(cts.Token);
            await svc.StopAsync(CancellationToken.None);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    ///     Тест 2: Сервис запускается при вебхуке.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Сервис запускается при вебхуке")]
    public async Task Should_StartWithWebhook()
    {
        // Arrange
        var svc = CreateService(TransportMode.Webhook);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var act = async () =>
        {
            await svc.StartAsync(cts.Token);
            await svc.StopAsync(CancellationToken.None);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static BotHostedService CreateService(TransportMode mode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeterFactory, DummyMeterFactory>();
        services.AddOptions<RateLimitOptions>().Configure(o =>
        {
            o.PerUserPerMinute = int.MaxValue;
            o.PerChatPerMinute = int.MaxValue;
            o.Mode = RateLimitMode.Soft;
        });
        services.AddOptions<DeduplicationOptions>().Configure(o => o.Window = TimeSpan.FromMinutes(5));
        services.AddSingleton(new TtlCache<string>(TimeSpan.FromMinutes(5)));
        services.AddSingleton<ITransportClient, DummyTransportClient>();
        services.AddSingleton(new HandlerRegistry());
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<MetricsMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<DedupMiddleware>();
        services.AddScoped<RateLimitMiddleware>();
        services.AddScoped<CommandParsingMiddleware>();
        services.AddScoped<RouterMiddleware>();
        services.AddSingleton<StatsCollector>();
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(Array.Empty<Action<IUpdatePipeline>>());
        services.AddSingleton<IUpdateSource, WaitingUpdateSource>();
        services.AddSingleton<ILogger<BotHostedService>>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<BotHostedService>());
        services.AddOptions<BotOptions>().Configure(o =>
        {
            o.Transport.Mode = mode;
            o.Transport.Parallelism = 1;
        });
        services.AddOptions<StopOptions>();

        var sp = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<BotHostedService>(sp);
    }

    private sealed class WaitingUpdateSource : IUpdateSource
    {
        public Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
        {
            var ctx = new UpdateContext(
                "test",
                "1",
                new ChatAddress(1),
                new UserAddress(1),
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                null!,
                ct);
            _ = onUpdate(ctx);
            var tcs = new TaskCompletionSource();
            ct.Register(() => tcs.TrySetResult());
            return tcs.Task;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class DummyTransportClient : ITransportClient
    {
        public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct) => Task.CompletedTask;
        public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct) => Task.CompletedTask;
        public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct) => Task.CompletedTask;
        public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct) => Task.CompletedTask;
        public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct) => Task.CompletedTask;
    }

    [SuppressMessage("Performance", "CA1822:MarkMembersAsStatic")]
    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options.Name, options.Version);
        public Meter Create(string name, string? version = null) => new(name, version);
        public void Dispose()
        {
        }
    }
}

