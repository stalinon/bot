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

## Настройки

* `BOT_TOKEN` — токен бота, обязателен;
* `Transport__Mode` — `Polling` или `Webhook` (по умолчанию `Polling`);
* `Transport__Webhook__PublicUrl` — требуется в режиме `Webhook`;
* `Admin__*` — параметры административного API при опции `--admin`;
* `WebApp__PublicUrl`, `WebApp__Secret` — параметры Mini App при опции `--webapp`.

## JetBrains Rider

После установки шаблона командой `dotnet new install` он появится в окне **New Solution** в Rider в разделе **Console**.
Перезапустите IDE и выберите `Bot`; шаблон отображается с собственной иконкой.
