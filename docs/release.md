# Релиз

## Упаковка NuGet-пакетов

Запустите скрипты сборки, чтобы получить пакеты с зафиксированными версиями зависимостей:

```bash
./scripts/pack.sh    # Linux и macOS
```

```powershell
pwsh scripts/pack.ps1 # Windows
```

Оба скрипта выполняют `dotnet restore --locked-mode` и `dotnet pack` в конфигурации `Release`.

## Локальная установка шаблонов

Для проверки шаблонов установите их локально:

```bash
dotnet new install templates/bot
```

или из собранного пакета:

```bash
dotnet new install ./templates/bin/Release/*.nupkg
```

## Публикация

1. Создайте тег с номером версии и отправьте его в репозиторий:
   ```bash
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```
2. Ожидайте завершения CI: пайплайн соберёт пакеты и опубликует релиз.
