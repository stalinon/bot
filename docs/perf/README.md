# Снимки производительности

Файлы `trace.nettrace` и `test.gcdump` исключены из репозитория из-за ограничений на бинарные артефакты.
Для повторного получения используйте:

```bash
dotnet-trace collect --output trace.nettrace -- dotnet test
dotnet-gcdump collect --output test.gcdump --process-id <PID>
```

После генерации сохраните артефакты локально для анализа.
