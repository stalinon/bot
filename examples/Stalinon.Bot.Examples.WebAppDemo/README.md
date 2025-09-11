# WebApp Demo

Пример минимального фронтенда для Telegram Mini App. Приложение выдаёт JWT через POST `/webapp/auth` и отправляет
введённый текст в бота через `WebApp.sendData`.

## Запуск

1. Установить .NET 8 SDK.
2. В каталоге `examples/Stalinon.Bot.Examples.WebAppDemo` задать переменные окружения:
   ```bash
   export Bot__Token="<токен бота>"
   export WebAppAuth__Secret="0123456789ABCDEF0123456789ABCDEF"
   export WebAppAuth__Lifetime="00:05:00"
   ```
3. Запустить приложение:
   ```bash
   dotnet run
   ```

Для доступа Telegram приложение должно быть опубликовано по HTTPS. Проще всего пробросить порт через `ngrok`:

```bash
ngrok http https://localhost:5001
```

Используйте выданный `https`‑URL в настройках веб-приложения бота.

## Проверка `sendData`

1. Добавьте кнопку Web App в бота (например, через `BotFather`).
2. Откройте Web App из чата с ботом.
3. Введите текст в форму и нажмите «Отправить».
4. Бот получит сообщение `web_app_data` с переданным текстом.

## Тесты

Интеграционные тесты `Stalinon.Bot.Examples.WebAppDemo.Tests` разворачивают приложение в памяти
с фейковым `Bot__Token` и заглушкой `IWebAppInitDataValidator`.
Проверяется отдача статической страницы и получение JWT через `POST /webapp/auth`.
