#!/usr/bin/env bash
set -euo pipefail

# Скрипт быстрый старт для шаблона tbot
# 1. Устанавливает локальный шаблон.
# 2. Генерирует проект с вебхуком и Mini App.
# 3. Запускает проект и проверяет /start и Mini App.

# Установка шаблона
TEMPLATE_DIR="$(dirname "$0")/../templates/tbot"
dotnet new install "$TEMPLATE_DIR" --force >/dev/null

# Генерация проекта в корне репозитория
PROJECT_DIR="quickstart-bot"
rm -rf "$PROJECT_DIR"
dotnet new tbot --transport webhook --webapp -n "$PROJECT_DIR" >/dev/null
cd "$PROJECT_DIR"
sed -i 's/{{transport}}/webhook/' appsettings.json
sed -i 's/{{store}}/file/' appsettings.json
sed -i '/UsePipeline(p => p/,+3 c\    .UsePipeline(_ => { })' Program.cs

# Запуск бота в фоне
BOT_TOKEN="1:1" Transport__Webhook__PublicUrl="" dotnet run >/tmp/qs.log 2>&1 &
PID=$!
# Ожидание старта
sleep 10

# Проверка /start
STATUS_START=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
  http://localhost:5000/tg/secret \
  -H 'Content-Type: application/json' \
  -d '{"update_id":1,"message":{"message_id":1,"date":0,"chat":{"id":1,"type":"private"},"from":{"id":1},"text":"/start"}}' || echo 000)

# Проверка Mini App
STATUS_WEBAPP=$(curl -s -o /dev/null -w "%{http_code}" \
  "http://localhost:5000/webapp/auth?initData=test" || echo 000)

# Завершение процесса
kill $PID >/dev/null 2>&1 || true
wait $PID 2>/dev/null || true
cd ..
rm -rf "$PROJECT_DIR"

# Итог
if [[ "$STATUS_START" -eq 200 && ( "$STATUS_WEBAPP" -eq 401 || "$STATUS_WEBAPP" -eq 400 ) ]]; then
  echo "Быстрый старт выполнен успешно"
else
  echo "Ошибка: /start=$STATUS_START webapp=$STATUS_WEBAPP" >&2
  exit 1
fi
