# Скрипт упаковки NuGet-пакетов с фиксированными версиями зависимостей
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root ".." "Bot.sln"

# Восстановление зависимостей в зафиксированном режиме
dotnet restore $solution --locked-mode

# Упаковка проектов
dotnet pack $solution -c $Configuration --no-restore -p:VersionSuffix=preview
