# WebhookBot

Пример бота, принимающего обновления через вебхук.
Использует те же обработчики, что и HelloBot: `/start`, `/ping`, `/typing` и обработчик неизвестного ввода.

## Запуск

1. Создайте бота в Telegram и получите токен.
2. Укажите токен в переменной окружения `BOT_TOKEN` или измените `appsettings.json`.
3. Опубликуйте локальный порт во внешний интернет, например через [ngrok](https://ngrok.com/):
   ```bash
    ngrok http 5000
    ```
   Скопируйте выданный HTTPS‑адрес и задайте его в переменной `Transport__Webhook__PublicUrl`.
4. При необходимости задайте свой секрет в `Transport__Webhook__Secret` (по умолчанию `secret`).
5. Установите вебхук:
   ```bash
   dotnet run --project examples/Stalinon.Bot.Examples.WebhookBot -- set-webhook
   ```
6. Запустите приложение:
   ```bash
   dotnet run --project examples/Stalinon.Bot.Examples.WebhookBot
   ```
7. Для удаления вебхука:
   ```bash
   dotnet run --project examples/Stalinon.Bot.Examples.WebhookBot -- delete-webhook
   ```

Бот слушает локальный порт 5000, а `Transport__Webhook__PublicUrl` должен указывать на этот адрес.

## Тесты

Интеграционные тесты `Stalinon.Bot.Examples.WebhookBot.Tests` разворачивают приложение в памяти
с фейковым `BOT_TOKEN` и `FakeTransportClient`.
Через `POST /tg/secret` отправляются обновления и проверяются ответы на команды `/start` и `/ping`.
