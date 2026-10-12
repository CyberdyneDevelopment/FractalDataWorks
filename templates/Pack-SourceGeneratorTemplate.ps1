#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs the Source Generator template using Nerdbank.GitVersioning.

.DESCRIPTION
    Uses nbgv to get the version from git, then packs the template as a NuGet package.

.PARAMETER OutputPath
    Where to output the packed template (default: ./nupkg)

.EXAMPLE
    .\Pack-Template.ps1

.EXAMPLE
    .\Pack-Template.ps1 -OutputPath "C:\packages"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "./nupkg"
)

$ErrorActionPreference = "Stop"

Write-Host "Packing Source Generator Template..." -ForegroundColor Cyan

# Check if nbgv is installed
if (!(Get-Command nbgv -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Nerdbank.GitVersioning tool..." -ForegroundColor Yellow
    dotnet tool install --global nbgv
}

# Get version from git (from solution root)
$solutionRoot = Join-Path $PSScriptRoot "..\"
Push-Location $solutionRoot
try {
    $version = nbgv get-version -v NuGetPackageVersion
    Write-Host "Version: $version (from solution root)" -ForegroundColor Green
} finally {
    Pop-Location
}

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# Pack using nuget
$templateRoot = Join-Path $PSScriptRoot "SourceGeneratorSolution"
$nuspecPath = Join-Path $templateRoot ".template.config\template.nuspec"
Write-Host "Packing from: $nuspecPath" -ForegroundColor Yellow

nuget pack $nuspecPath `
    -OutputDirectory $OutputPath `
    -Version $version `
    -BasePath $templateRoot `
    -NoPackageAnalysis

if ($LASTEXITCODE -eq 0) {
    $packagePath = Join-Path $OutputPath "Fdw.Templates.SourceGenerator.$version.nupkg"
    Write-Host ""
    Write-Host "Template packed successfully!" -ForegroundColor Green
    Write-Host "Package: $packagePath" -ForegroundColor White
    Write-Host ""
    Write-Host "To install:" -ForegroundColor Cyan
    Write-Host "  dotnet new install $packagePath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or install from directory:" -ForegroundColor Cyan
    Write-Host "  dotnet new install $templateRoot" -ForegroundColor Gray
} else {
    Write-Error "Failed to pack template"
}
