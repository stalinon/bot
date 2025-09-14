using System.Diagnostics;
using System.IO;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Observability;

using Telegram.Bot;

namespace Stalinon.Bot.Observability.Tests;

/// <summary>
///     Тесты подключения наблюдаемости: проверка включения и отключения провайдеров.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет, что провайдеры создаются при включённом OTLP.</item>
///         <item>Проверяет, что провайдеры не создаются без экспортёров.</item>
///     </list>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    /// <inheritdoc/>
    public ServiceCollectionExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен создавать провайдеры при включённом OTLP.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен создавать провайдеры при включённом OTLP")]
    public void Should_CreateProviders_When_OtlpEnabled()
    {
        Environment.SetEnvironmentVariable("OBS__EXPORT__OTLP", "1");
        var services = new ServiceCollection().AddObservability();
        var sp = services.BuildServiceProvider();
        sp.GetService<TracerProvider>().Should().NotBeNull();
        sp.GetService<MeterProvider>().Should().NotBeNull();
        Environment.SetEnvironmentVariable("OBS__EXPORT__OTLP", null);
    }

    /// <summary>
    ///     Тест 2: Должен не создавать провайдеры при отсутствии экспортёров.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен не создавать провайдеры при отсутствии экспортёров")]
    public void Should_NotCreateProviders_When_NoExporters()
    {
        Environment.SetEnvironmentVariable("OBS__EXPORT__OTLP", null);
        var services = new ServiceCollection().AddObservability();
        var sp = services.BuildServiceProvider();
        sp.GetService<TracerProvider>().Should().BeNull();
        sp.GetService<MeterProvider>().Should().BeNull();
    }

    /// <summary>
    ///     Тест 3: Должен регистрировать ActivitySource Telemetry.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен регистрировать ActivitySource Telemetry.")]
    public void Should_RegisterTelemetryActivitySource()
    {
        // Arrange
        var services = new ServiceCollection().AddObservability();
        var sp = services.BuildServiceProvider();

        // Act
        var source = sp.GetRequiredService<ActivitySource>();

        // Assert
        source.Should().Be(Telemetry.ActivitySource);
    }

    /// <summary>
    ///     Тест 4: Должен регистрировать TracingStateStore.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен регистрировать TracingStateStore.")]
    public void Should_RegisterTracingStateStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStateStore, DummyStateStore>();

        // Act
        services.AddObservability();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IStateStore>().Should().BeOfType<TracingStateStore>();
    }

    /// <summary>
    ///     Тест 5: Должен регистрировать TracingTransportClient.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен регистрировать TracingTransportClient.")]
    public void Should_RegisterTracingTransportClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITransportClient, DummyTransportClient>();

        // Act
        services.AddObservability();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<ITransportClient>().Should().BeOfType<TracingTransportClient>();
    }

    private sealed class DummyStateStore : IStateStore
    {
        public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct) => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl, CancellationToken ct) => Task.FromResult(false);

        public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct) => Task.FromResult(false);

        public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct) => Task.FromResult(0L);

        public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class DummyTransportClient : ITransportClient
    {
        public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct) => Task.CompletedTask;

        public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct) => Task.CompletedTask;

        public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct) => Task.CompletedTask;

        public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct) => Task.CompletedTask;

        public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct) => Task.CompletedTask;

        public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct) => Task.CompletedTask;

        public Task SendPollAsync(ChatAddress chat, string question, IEnumerable<string> options, bool allowsMultipleAnswers, CancellationToken ct) => Task.CompletedTask;

        public Task SetMessageReactionAsync(ChatAddress chat, long messageId, IEnumerable<string> reactions, bool isBig, CancellationToken ct) => Task.CompletedTask;

        public Task CallNativeClientAsync(Func<ITelegramBotClient, CancellationToken, Task> action, CancellationToken ct) => Task.CompletedTask;
    }
}
