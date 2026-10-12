# Install Fdw Item Templates
# Run this script from the templates directory

$ErrorActionPreference = "Stop"

Write-Host "Installing Fdw Item Templates..." -ForegroundColor Cyan

# Install dotnet template
Write-Host "`nInstalling MessageLogger template for dotnet CLI..." -ForegroundColor Yellow
dotnet new install .\MessageLogger

Write-Host "`nTemplate installation complete!" -ForegroundColor Green
Write-Host "`nUsage:" -ForegroundColor Cyan
Write-Host "  dotnet new fdw-logger --help" -ForegroundColor White
Write-Host "  dotnet new fdw-logger --loggerName MyServiceLogger" -ForegroundColor White
Write-Host "  dotnet new fdw-logger --namespace MyCompany.Services --loggerName ApiLogger" -ForegroundColor White

# List installed templates
Write-Host "`nVerifying installation..." -ForegroundColor Yellow
dotnet new list | Select-String -Pattern "fdw-logger"
