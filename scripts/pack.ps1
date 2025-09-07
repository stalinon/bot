<#
    Скрипт упаковки NuGet-пакетов с фиксированными версиями зависимостей
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Join-Path -Path $root -ChildPath '..'

$projects = Get-ChildItem -Path (Join-Path $repo 'src') -Directory |
    ForEach-Object { Get-ChildItem -Path $_.FullName -Filter *.csproj }
$projects += Get-Item (Join-Path $repo 'templates/Bot.Templates.csproj')

# Восстановление зависимостей в зафиксированном режиме
dotnet restore @($projects.FullName) --locked-mode

# Упаковка проектов
foreach ($project in $projects) {
    dotnet pack $project.FullName -c $Configuration --no-restore -p:VersionSuffix=preview
}
