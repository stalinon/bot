# HelloBot

Простой пример телеграм‑бота на этой библиотеке.
Демонстрирует обработку нескольких команд и хранение состояния.

## Что делает

- `/start` — приветствует пользователя и предлагает написать `/ping`.
- `/ping` — отвечает `pong` и ведёт счёт для каждого пользователя, используя файловое хранилище состояний.
- `/typing` — показывает индикатор печати перед ответом.
- `/phone` — запускает ввод номера телефона с подтверждением.
- Любой другой ввод — сообщение «не понимаю :(».

## Конфигурация

* `BOT_TOKEN` — токен бота, обязателен;
* `Transport__Mode` — `Polling` или `Webhook` (по умолчанию `Polling`);
* `Transport__Webhook__PublicUrl` — публичный URL для режима `Webhook`;
* `PHONE_STEP_TTL_SECONDS` — тайм‑аут ввода телефона, секунд.

## Запуск

1. Создайте бота в Telegram и получите токен.
2. Укажите токен в переменной окружения `BOT_TOKEN` или измените `appsettings.json`.
3. Запустите пример:

```bash
dotnet run --project examples/Stalinon.Bot.Examples.HelloBot
```

По умолчанию бот получает обновления через long polling.
Для работы через вебхуки доступны команды:

```bash
dotnet run --project examples/Stalinon.Bot.Examples.HelloBot -- set-webhook
dotnet run --project examples/Stalinon.Bot.Examples.HelloBot -- delete-webhook
```

Время ожидания ввода телефона задаётся параметром `PHONE_STEP_TTL_SECONDS`.
