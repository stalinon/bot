# Stalinon.Bot.Examples.HelloBot.Tests

Тестовый проект для библиотеки `Stalinon.Bot.Examples.HelloBot`.

## Назначение
Проверяет основные сценарии и граничные случаи `Stalinon.Bot.Examples.HelloBot`.

### Интеграционные проверки
В каталоге `Integration` разворачивается `HelloBot` в памяти с фейковым `BOT_TOKEN` и `FakeTransportClient`.
Проверяются ответы на команды `/start`, `/ping` и сценарий `/phone`, а также исключение при отсутствии `BOT_TOKEN`.

## Запуск
```bash
dotnet test
```
