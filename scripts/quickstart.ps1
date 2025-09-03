#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Скрипт быстрый старт для шаблона tbot
# 1. Устанавливает локальный шаблон.
# 2. Генерирует проект с вебхуком и Mini App.
# 3. Запускает проект и проверяет /start и Mini App.

# Установка шаблона
$templ = Join-Path $PSScriptRoot '..' 'templates' 'tbot'
dotnet new install $templ --force | Out-Null

# Генерация проекта в корне репозитория
$project = Join-Path (Get-Location) 'quickstart-bot'
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $project
$null = dotnet new tbot --transport webhook --webapp -n 'quickstart-bot'
Set-Location $project
(Get-Content appsettings.json) -replace '{{transport}}','webhook' -replace '{{store}}','file' | Set-Content appsettings.json
(Get-Content Program.cs) -replace 'UsePipeline\(p => p[\s\S]*?UseConfiguredStateStorage', 'UsePipeline(_ => { })`n    .UseConfiguredStateStorage' | Set-Content Program.cs

# Запуск бота
$env:BOT_TOKEN = '1:1'
$env:Transport__Webhook__PublicUrl = ''
$proc = Start-Process dotnet -ArgumentList 'run' -PassThru -RedirectStandardOutput run.log -RedirectStandardError err.log
Start-Sleep -Seconds 10

# Проверка /start
$body = '{"update_id":1,"message":{"message_id":1,"date":0,"chat":{"id":1,"type":"private"},"from":{"id":1},"text":"/start"}}'
$startStatus = try {
    (Invoke-WebRequest -Uri 'http://localhost:5000/tg/secret' -Method Post -Body $body -ContentType 'application/json').StatusCode.value__
} catch { 0 }

# Проверка Mini App
$webappStatus = try {
    (Invoke-WebRequest -Uri 'http://localhost:5000/webapp/auth?initData=test').StatusCode.value__
} catch { $_.Exception.Response.StatusCode.value__ }

# Завершение процесса
Stop-Process $proc.Id
$proc.WaitForExit()
Set-Location ..
Remove-Item -Recurse -Force $project

# Итог
if ($startStatus -eq 200 -and ($webappStatus -eq 401 -or $webappStatus -eq 400)) {
    Write-Host 'Быстрый старт выполнен успешно'
} else {
    Write-Error "Ошибка: /start=$startStatus webapp=$webappStatus"
    exit 1
}
