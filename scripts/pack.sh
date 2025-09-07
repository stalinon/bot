#!/usr/bin/env bash
# Скрипт упаковки NuGet-пакетов с фиксированными версиями зависимостей
set -euo pipefail

CONFIGURATION=${1:-Release}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="$SCRIPT_DIR/../Bot.sln"

dotnet restore "$SOLUTION" --locked-mode
dotnet pack "$SOLUTION" -c "$CONFIGURATION" --no-restore
