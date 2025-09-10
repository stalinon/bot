# Stalinon.Bot.Templates

Единый шаблон `bot` позволяет сконфигурировать способ получения обновлений,
хранилище и дополнительные функции.

## Использование

```bash
dotnet new install Stalinon.Bot.Templates
dotnet new bot --name MyBot [параметры]
```

## Параметры

* `--transport polling|webhook` — способ получения обновлений (по умолчанию polling);
* `--public-url <url>` — публичный URL для вебхука;
* `--webhook-secret <строка>` — секрет вебхука (по умолчанию случайный GUID);
* `--store file|redis|ef` — хранилище состояния (`file` по умолчанию);
* `--redis <строка>` — строка подключения Redis;
* `--ef-provider postgres|sqlite` — провайдер EF Core;
* `--ef-conn <строка>` — строка подключения EF Core;
* `--admin` — включить административное API;
* `--webapp` — добавить точки Mini App;
* `--lang ru|en` — язык исходников и комментариев;
* `--bot-token <токен>` — токен бота (можно задать позже через `BOT_TOKEN`).

## JetBrains Rider

После установки шаблона командой `dotnet new install` он будет доступен в окне **New Solution** в Rider.
Перезапустите IDE и выберите `bot` в списке шаблонов .NET.
