using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Hosting.Tests;

/// <summary>
///     Тесты регистрации сервисов хостинга.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется добавление <see cref="BotHostedService" /> в коллекцию.</item>
///         <item>Проверяется регистрация <see cref="StatsCollector" />.</item>
///     </list>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    /// <inheritdoc />
    public ServiceCollectionExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует <see cref="BotHostedService" />.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует BotHostedService")]
    public void Should_RegisterBotHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUpdateSource, DummyUpdateSource>();
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(Array.Empty<Action<IUpdatePipeline>>());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        // Act
        services.AddBot(_ => { });
        using var sp = services.BuildServiceProvider();

        // Assert
        sp.GetServices<IHostedService>().Should().ContainSingle(s => s is BotHostedService);
    }

    /// <summary>
    ///     Тест 2: Регистрирует сборщик статистики.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Регистрирует сборщик статистики")]
    public void Should_RegisterStatsCollector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IUpdateSource, DummyUpdateSource>();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(Array.Empty<Action<IUpdatePipeline>>());

        // Act
        services.AddBot(_ => { });
        using var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<StatsCollector>().Should().NotBeNull();
    }
}
