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

### Quickstart scripts

Use `scripts/quickstart.sh` or `scripts/quickstart.ps1` to validate the template automatically. The script installs the template, runs a generated bot and checks `/start`, obtains a JWT via `/webapp/auth`, requests `/webapp/me` and sends `web_app_data` to ensure the handler responds.

### Переключение хранилища

Тип хранилища задаётся переменной окружения `Storage__Provider` (`file`, `redis` или `ef`).
Другие параметры берутся из секции `Storage`.

### Административное API

Административное API включено по умолчанию. Для запросов требуется заголовок `X-Admin-Token`,
значение берётся из секции `Admin` или переменной окружения `Admin__AdminToken`.

### Документация

* [Mini App](docs/miniapps.md)

### Настройки Mini App

Мини-приложение настраивается через переменные окружения:

* `WebApp__PublicUrl` — публичный URL страницы.
* `WebApp__AuthTtlSeconds` — срок жизни JWT в секундах.
* `WebApp__InitDataTtlSeconds` — время жизни параметра `initData` в секундах.
* `WebApp__Csp__AllowedOrigins__0` — дополнительный origin для Content-Security-Policy.

### Лидерборды на Redis

Для хранения рейтингов можно использовать отсортированные множества:

```csharp
using Bot.Storage.Redis;

var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "lb" };
var board = new RedisSortedSet<Player>(options);

await board.AddAsync("game", new Player("Alice"), 1200, ct);
await board.AddAsync("game", new Player("Bob"), 1500, ct);

var top = await board.RangeByScoreAsync("game", 0, double.MaxValue, ct);
```

```csharp
public sealed record Player(string Name);
```
