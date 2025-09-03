#!/usr/bin/env bash
set -euo pipefail

# Скрипт быстрый старт для шаблона tbot
# 1. Устанавливает локальный шаблон.
# 2. Генерирует проект с вебхуком, Mini App и EF Core хранилищем.
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
sed -i 's/{{store}}/ef/' appsettings.json
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

# Подготовка initData для Mini App
NOW=$(date +%s)
DATA_CHECK="auth_date=$NOW\nquery_id=1\nuser={\"id\":1}"
SECRET=$(printf 'WebAppData' | openssl dgst -sha256 -hmac "$BOT_TOKEN" -binary | xxd -p -c 256)
HASH=$(printf "$DATA_CHECK" | openssl dgst -sha256 -mac HMAC -macopt hexkey:$SECRET | awk '{print $2}')
INIT_DATA="auth_date=$NOW&query_id=1&user=%7B%22id%22%3A1%7D&hash=$HASH"

# Получение JWT
AUTH_RESP=$(curl -s -k -w "%{http_code}" -o auth.json -X POST https://localhost:5001/webapp/auth \
  -H 'Content-Type: application/json' \
  -d "{\"initData\":\"$INIT_DATA\"}" || echo 000)
STATUS_AUTH="$AUTH_RESP"
TOKEN=$(grep -o '"token":"[^"]*"' auth.json | cut -d'"' -f4)

# Проверка профиля
STATUS_ME=$(curl -s -k -o /dev/null -w "%{http_code}" https://localhost:5001/webapp/me \
  -H "Authorization: Bearer $TOKEN" || echo 000)

# Отправка web_app_data
STATUS_DATA=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
  http://localhost:5000/tg/secret \
  -H 'Content-Type: application/json' \
  -d '{"update_id":2,"message":{"message_id":2,"date":0,"chat":{"id":1,"type":"private"},"from":{"id":1},"web_app_data":{"data":"42"}}}' || echo 000)

# Завершение процесса
kill $PID >/dev/null 2>&1 || true
wait $PID 2>/dev/null || true
rm -f bot_state.db
cd ..
rm -rf "$PROJECT_DIR"

# Итог
if [[ "$STATUS_START" -eq 200 && "$STATUS_AUTH" -eq 200 && "$STATUS_ME" -eq 200 && "$STATUS_DATA" -eq 200 ]]; then
  echo "Быстрый старт выполнен успешно"
else
  echo "Ошибка: /start=$STATUS_START auth=$STATUS_AUTH me=$STATUS_ME sendData=$STATUS_DATA" >&2
  exit 1
fi
