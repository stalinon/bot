using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests;

/// <summary>
///     Тесты биндинга конфигурации.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется бинд QUEUE__*.</item>
///         <item>Проверяется бинд STOP__*.</item>
///         <item>Проверяется бинд OBS__*.</item>
///         <item>Проверяется бинд OUTBOX__*.</item>
///     </list>
/// </remarks>
public sealed class ConfigurationBindingTests
{
    /// <inheritdoc />
    public ConfigurationBindingTests()
    {
    }

    /// <summary>
    ///     Тест 1: Биндит QUEUE__POLICY в QueueOptions.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Биндит QUEUE__POLICY в QueueOptions")]
    public void Should_BindQueueOptions()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Queue:Policy"] = "Drop"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);

        // Act
        services.AddBot(_ => { });
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<QueueOptions>>().Value;
        options.Policy.Should().Be(QueuePolicy.Drop);
    }

    /// <summary>
    ///     Тест 2: Биндит STOP__DRAIN_TIMEOUT_SECONDS в StopOptions.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Биндит STOP__DRAIN_TIMEOUT_SECONDS в StopOptions")]
    public void Should_BindStopOptions()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stop:DRAIN_TIMEOUT_SECONDS"] = "10"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);

        // Act
        services.AddBot(_ => { });
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<StopOptions>>().Value;
        options.DrainTimeoutSeconds.Should().Be(10);
    }

    /// <summary>
    ///     Тест 3: Биндит OBS__EXPORT__OTLP в ObservabilityOptions.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Биндит OBS__EXPORT__OTLP в ObservabilityOptions")]
    public void Should_BindObservabilityOptions()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Obs:Export:Otlp"] = "true"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);

        // Act
        services.AddBot(_ => { });
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.Export.Otlp.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 4: Биндит OUTBOX__PATH в OutboxOptions.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Биндит OUTBOX__PATH в OutboxOptions")]
    public void Should_BindOutboxOptions()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:Path"] = "/tmp/outbox"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);

        // Act
        services.AddBot(_ => { });
        var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;
        options.Path.Should().Be("/tmp/outbox");
    }
}

