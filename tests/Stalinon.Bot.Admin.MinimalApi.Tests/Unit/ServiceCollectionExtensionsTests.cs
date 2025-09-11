using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Admin.MinimalApi.Tests;

/// <summary>
///     Тесты регистрации служб административного API.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяет регистрацию всех health-проб.</item>
///         <item>Проверяет регистрацию сборщиков статистики.</item>
///     </list>
/// </remarks>
public sealed class ServiceCollectionExtensionsTests
{
    /// <inheritdoc />
    public ServiceCollectionExtensionsTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен регистрировать все health-пробы.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен регистрировать все health-пробы.")]
    public void Should_RegisterAllProbes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddAdminApi(configuration);
        var probes = services.Where(d => d.ServiceType == typeof(IHealthProbe)).ToList();

        // Assert
        probes.Should().HaveCount(3);
        probes.Should().Contain(d => d.ImplementationType == typeof(TransportProbe));
        probes.Should().Contain(d => d.ImplementationType == typeof(QueueProbe));
        probes.Should().Contain(d => d.ImplementationType == typeof(StorageProbe));
    }

    /// <summary>
    ///     Тест 2: Должен регистрировать сборщики статистики.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен регистрировать сборщики статистики.")]
    public void Should_RegisterStatsCollectors()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddAdminApi(configuration);
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<StatsCollector>().Should().NotBeNull();
        sp.GetService<WebAppStatsCollector>().Should().NotBeNull();
        sp.GetService<CustomStats>().Should().NotBeNull();
    }
}
