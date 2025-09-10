# Stalinon.Bot.Telegram

Интеграция с Telegram Bot API.

## Возможности
- источники обновлений через polling или webhook;
- клиент транспорта и сервисы меню чата;
- вспомогательные компоненты для Web App.

## Использование
```csharp
services.AddTelegramTransport();
```
Токен бота задаётся в `BotOptions.Token`.
