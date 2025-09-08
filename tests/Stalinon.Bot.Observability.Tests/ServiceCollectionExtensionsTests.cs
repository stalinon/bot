using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Stalinon.Bot.Observability;

using Xunit;

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
}
