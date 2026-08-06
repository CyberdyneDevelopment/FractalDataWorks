# Uninstall Fdw Item Templates

$ErrorActionPreference = "Stop"

Write-Host "Uninstalling Fdw Item Templates..." -ForegroundColor Cyan

# Uninstall dotnet template
Write-Host "`nUninstalling MessageLogger template..." -ForegroundColor Yellow
dotnet new uninstall .\MessageLogger

Write-Host "`nTemplate uninstallation complete!" -ForegroundColor Green
