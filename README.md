# Bot

Минимальный фреймворк для создания Telegram-ботов на .NET.

## Быстрый старт

1. Установите **.NET 8 SDK**.
2. Клонируйте репозиторий и перейдите в каталог проекта:
   ```bash
   git clone <repo-url>
   cd bot
   ```
3. Создайте бота через [@BotFather](https://t.me/BotFather) и получите токен.
4. Запустите пример **HelloBot** с вашим токеном:
   ```bash
   cd Bot.Examples.HelloBot
   export BOT_TOKEN="<ваш-токен>"
   dotnet run
   ```
5. Отправьте `/start` в Telegram — бот ответит «Hello».

Настройки можно менять в `appsettings.json` или через переменные окружения.

### Скрипты быстрого старта

`scripts/quickstart.sh` и `scripts/quickstart.ps1` проверяют работоспособность шаблона: 
устанавливают его, запускают сгенерированный бот и вызывают набор тестовых команд.

### Переключение хранилища

Тип хранилища выбирается переменной `Storage__Provider` (`file`, `redis` или `ef`).
Прочие параметры задаются в секции `Storage`.

### Административное API

API включено по умолчанию. Для запросов необходим заголовок `X-Admin-Token`,
значение берётся из секции `Admin` или переменной `Admin__AdminToken`.

### Документация

* [Mini App](docs/miniapps.md)
* [Сцены](docs/scenes.md)

### Настройки Mini App

* `WebApp__PublicUrl` — публичный URL страницы;
* `WebApp__AuthTtlSeconds` — срок жизни JWT в секундах;
* `WebApp__InitDataTtlSeconds` — время жизни параметра `initData`;
* `WebApp__Csp__AllowedOrigins__0` — дополнительный origin для CSP.

### Лидерборды на Redis

```csharp
using Bot.Storage.Redis;

var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "lb" };
var board = new RedisSortedSet<Player>(options);

await board.AddAsync("game", new Player("Alice"), 1200, ct);
await board.AddAsync("game", new Player("Bob"), 1500, ct);

var top = await board.RangeByScoreAsync("game", 0, double.MaxValue, ct);

public sealed record Player(string Name);
```

Значения сериализуются в JSON и сохраняются с указанным префиксом ключей.
