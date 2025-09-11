# Stalinon.Bot.Examples.WebhookBot.Tests

Тестовый проект для библиотеки `Stalinon.Bot.Examples.WebhookBot`.

## Назначение
Проверяет обработку команд через вебхук.

Тесты находятся в каталоге `Integration`.

### Интеграционные проверки
В каталоге `Integration` бот разворачивается в памяти с фейковым `BOT_TOKEN` и `FakeTransportClient`.
Через `POST /tg/secret` отправляются обновления `/start` и `/ping`.

## Запуск
```bash
dotnet test
```
