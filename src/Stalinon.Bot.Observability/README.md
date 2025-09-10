# Stalinon.Bot.Observability

Подключает метрики и трассировку через OpenTelemetry.

## Возможности
- регистрация провайдеров метрик и трасс;
- OTLP-экспорт через переменную `OBS__EXPORT__OTLP`.

## Использование
```csharp
services.AddObservability();
```
При необходимости можно передать делегаты `configureMeter` и `configureTracer`.
