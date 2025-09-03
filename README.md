# Bot

A minimal framework for building Telegram bots.

## Quickstart

These steps will run the sample **HelloBot** in a couple of minutes.

1. **Install .NET 8 SDK** if you don't have it.
2. **Clone** this repository and enter the folder:
   ```bash
   git clone <repo-url>
   cd bot
   ```
3. **Create a Telegram bot** with [@BotFather](https://t.me/BotFather) and copy the token.
4. **Run the example** with your token:
   ```bash
   cd Bot.Examples.HelloBot
   export BOT_TOKEN="<your-token>"
   dotnet run
   ```
5. Send `/start` to your bot in Telegram and it will reply with "Hello".

For development or deployment you can adjust settings in `appsettings.json` or via environment variables.

### Переключение хранилища

Тип хранилища задаётся переменной окружения `Storage__Provider` (`file`, `redis` или `ef`).
Другие параметры берутся из секции `Storage`.

### Документация

* [Mini App](docs/miniapps.md)

### Настройки Mini App

Мини-приложение настраивается через переменные окружения:

* `WebApp__PublicUrl` — публичный URL страницы.
* `WebApp__AuthTtlSeconds` — срок жизни JWT в секундах.
* `WebApp__InitDataTtlSeconds` — время жизни параметра `initData` в секундах.
* `WebApp__Csp__AllowedOrigins__0` — дополнительный origin для Content-Security-Policy.
