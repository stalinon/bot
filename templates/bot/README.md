# Bot

Пример бота на Telegram с гибкой конфигурацией.

## Быстрый старт

1. Установите пакет шаблонов: `dotnet new install Bot.Templates`.
2. Создайте проект: `dotnet new bot --name MyBot`.
3. Заполните `.env` на основе `.env.sample` и запустите приложение.

## Параметры

* `--transport polling|webhook` — способ получения обновлений (по умолчанию polling);
* `--store file|redis|ef` — хранилище состояния (`file` по умолчанию);
* `--admin` — включить административное API;
* `--webapp` — добавить точки Mini App;
* `--lang ru|en` — язык исходников и комментариев.
