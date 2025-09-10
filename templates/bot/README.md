# Bot

Пример бота на Telegram с гибкой конфигурацией.

## Быстрый старт

1. Установите пакет шаблонов: `dotnet new install Stalinon.Bot.Templates`.
2. Создайте проект: `dotnet new bot --name MyBot`.
3. Заполните `.env` на основе `.env.sample` и запустите приложение.

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
* `Transport__Webhook__PublicUrl` — публичный URL при режиме `Webhook`;
* `Admin__*` — параметры административного API при включённой опции `--admin`;
* `WebApp__PublicUrl`, `WebApp__Secret` — параметры Mini App при включении `--webapp`.

## JetBrains Rider

После установки шаблона через `dotnet new install` он появится в окне **New Solution**.
Перезапустите Rider и выберите `bot` среди доступных .NET шаблонов.
