#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Скрипт быстрый старт для шаблона bot
# 1. Устанавливает локальный шаблон.
# 2. Генерирует проект с вебхуком, Mini App и EF Core хранилищем.
# 3. Запускает проект и проверяет /start и Mini App.

# Установка шаблона
$templ = Join-Path $PSScriptRoot '..' 'templates' 'bot'
dotnet new install $templ --force | Out-Null

# Генерация проекта в корне репозитория
$project = Join-Path (Get-Location) 'quickstart-bot'
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $project
$null = dotnet new bot --transport webhook --webapp -n 'quickstart-bot'
Set-Location $project
(Get-Content appsettings.json) -replace '{{transport}}','webhook' -replace '{{store}}','ef' | Set-Content appsettings.json
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

# Подготовка initData для Mini App
function New-InitData {
    param([string]$token)
    $fields = [ordered]@{
        auth_date = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
        query_id = '1'
        user = '{"id":1}'
    }
    $dataCheckString = ($fields.GetEnumerator() | Sort-Object Name | ForEach-Object {"$($_.Name)=$($_.Value)"}) -join "`n"
    $secretHmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes('WebAppData'))
    $secret = $secretHmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($token))
    $hmac = [System.Security.Cryptography.HMACSHA256]::new($secret)
    $hashBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($dataCheckString))
    $hash = ($hashBytes | ForEach-Object { "{0:x2}" -f $_ }) -join ''
    $userEncoded = [System.Net.WebUtility]::UrlEncode($fields.user)
    return "auth_date=$($fields.auth_date)&query_id=1&user=$userEncoded&hash=$hash"
}
$initData = New-InitData $env:BOT_TOKEN

# Получение JWT
$authResp = Invoke-WebRequest -Uri 'https://localhost:5001/webapp/auth' -Method Post -Body (@{ initData = $initData } | ConvertTo-Json) -ContentType 'application/json' -SkipCertificateCheck
$authStatus = $authResp.StatusCode.value__
$token = (ConvertFrom-Json $authResp.Content).token

# Проверка профиля
$headers = @{ Authorization = "Bearer $token" }
$meStatus = (Invoke-WebRequest -Uri 'https://localhost:5001/webapp/me' -Headers $headers -SkipCertificateCheck).StatusCode.value__

# Отправка web_app_data
$dataBody = '{"update_id":2,"message":{"message_id":2,"date":0,"chat":{"id":1,"type":"private"},"from":{"id":1},"web_app_data":{"data":"42"}}}'
$dataStatus = try {
    (Invoke-WebRequest -Uri 'http://localhost:5000/tg/secret' -Method Post -Body $dataBody -ContentType 'application/json').StatusCode.value__
} catch { 0 }

# Завершение процесса
Stop-Process $proc.Id
$proc.WaitForExit()
Remove-Item -Force -ErrorAction SilentlyContinue bot_state.db
Set-Location ..
Remove-Item -Recurse -Force $project

# Итог
if ($startStatus -eq 200 -and $authStatus -eq 200 -and $meStatus -eq 200 -and $dataStatus -eq 200) {
    Write-Host 'Быстрый старт выполнен успешно'
} else {
    Write-Error "Ошибка: /start=$startStatus auth=$authStatus me=$meStatus sendData=$dataStatus"
    exit 1
}
