# Bot.Templates

Единый шаблон `bot` позволяет сконфигурировать способ получения обновлений,
хранилище и дополнительные функции.

## Использование

```bash
dotnet new install Bot.Templates
dotnet new bot --name MyBot [параметры]
```

## Параметры

* `--transport polling|webhook` — способ получения обновлений (по умолчанию polling);
* `--store file|redis|ef` — хранилище состояния (`file` по умолчанию);
* `--admin` — включить административное API;
* `--webapp` — добавить точки Mini App;
* `--lang ru|en` — язык исходников и комментариев.

