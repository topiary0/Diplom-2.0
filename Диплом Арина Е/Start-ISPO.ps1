$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'ISPO.WebApp\ISPO.WebApp.csproj'

Write-Host 'Останавливаю предыдущие процессы ISPO.WebApp...'
Get-Process ISPO.WebApp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host 'Запускаю приложение на http://127.0.0.1:61990 ...'
dotnet run --no-launch-profile --project $project --urls "http://127.0.0.1:61990"
