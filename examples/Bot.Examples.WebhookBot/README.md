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
   Скопируйте выданный HTTPS‑адрес и задайте его в переменной `PUBLIC_URL`.
4. При необходимости задайте свой секрет в `WEBHOOK_SECRET` (по умолчанию `secret`).
5. Установите вебхук:
   ```bash
   dotnet run --project examples/Bot.Examples.WebhookBot -- set-webhook
   ```
6. Запустите приложение:
   ```bash
   dotnet run --project examples/Bot.Examples.WebhookBot
   ```
7. Для удаления вебхука:
   ```bash
   dotnet run --project examples/Bot.Examples.WebhookBot -- delete-webhook
   ```

Бот слушает локальный порт 5000, а `PUBLIC_URL` должен указывать на этот адрес.
