using System.Diagnostics;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Observability;

/// <summary>
///     Расширения для настройки наблюдаемости.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Подключают провайдеры метрик и трассировки.</item>
///         <item>Настраивают OTLP-экспортёр через переменную окружения.</item>
///     </list>
/// </remarks>
public static class ServiceCollectionExtensions
{
    private const string OtlpVar = "OBS__EXPORT__OTLP";

    /// <summary>
    ///     Добавить наблюдаемость.
    /// </summary>
    /// <param name="configureMeter">Дополнительная настройка метрик.</param>
    /// <param name="configureTracer">Дополнительная настройка трассировки.</param>
    public static IServiceCollection AddObservability(this IServiceCollection services,
        Action<MeterProviderBuilder>? configureMeter = null,
        Action<TracerProviderBuilder>? configureTracer = null)
    {
        var otlp = IsEnabled(OtlpVar);
        var builder = services.AddOpenTelemetry();

        if (otlp)
        {
            builder.WithMetrics(mb =>
            {
                mb.AddMeter("Stalinon.Bot.Core.Metrics");
                mb.AddOtlpExporter();
                configureMeter?.Invoke(mb);
            });

            builder.WithTracing(tb =>
            {
                tb.AddSource(Telemetry.ActivitySourceName);
                tb.AddOtlpExporter();
                configureTracer?.Invoke(tb);
            });
        }

        services.AddSingleton(Telemetry.ActivitySource);
        Decorate<IStateStore, TracingStateStore>(services);
        Decorate<ITransportClient, TracingTransportClient>(services);

        return services;
    }

    private static bool IsEnabled(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value)
               && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private static void Decorate<TService, TDecorator>(IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is null)
        {
            return;
        }

        if (descriptor.ImplementationType == typeof(TDecorator)
            || descriptor.ImplementationInstance is TDecorator)
        {
            return;
        }

        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(typeof(TService), sp =>
        {
            var inner = descriptor.ImplementationInstance ??
                        descriptor.ImplementationFactory?.Invoke(sp) ??
                        ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
            return ActivatorUtilities.CreateInstance<TDecorator>(sp, inner);
        }, descriptor.Lifetime));
    }
}
