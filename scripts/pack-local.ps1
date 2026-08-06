# Pack Fdw packages to local NuGet folder.
# PowerShell equivalent of pack-local.sh (without the GitLab push step —
# run push-gitlab.sh / push-gitlab.ps1 separately for that).
# Requires: LocalNugetFolder environment variable
# Usage: .\pack-local.ps1 [-NoBuild] [-Configuration Release] [-ConfigName Fdw.Local.nuget.config]

param(
    [Parameter(Position = 0)]
    [string]$ConfigName = "Fdw.Local.nuget.config",

    [Alias("n")]
    [switch]$NoBuild,

    [Alias("c")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Validate environment
if (!$env:LocalNugetFolder) {
    Write-Error "LocalNugetFolder environment variable not set. Set it to your local NuGet folder path (e.g., C:\development\local-nuget)"
    exit 1
}

$localNuget = $env:LocalNugetFolder
$rootDir = (Resolve-Path "$PSScriptRoot\..").Path

# Why: delete any prior pack-errors file up front so its presence after a run
# unambiguously means "this run produced error/warning summary lines".
$packErrLog = Resolve-Path -Path (Join-Path $rootDir "..") -ErrorAction SilentlyContinue
if ($packErrLog) {
    $packErrLog = Join-Path $packErrLog.Path "last-pack-errors.txt"
} else {
    $packErrLog = Join-Path (Split-Path $rootDir -Parent) "last-pack-errors.txt"
}
if (Test-Path $packErrLog) { Remove-Item $packErrLog -Force -ErrorAction SilentlyContinue }

# Why: stale MSBuild / VBCSCompiler / Razor build-server workers from prior runs
# linger as zombie processes — observed 11 leftover MSBuild nodes consuming 70%+
# of CPU between pack invocations. Shut them down so this run starts with a clean
# pool instead of competing with stale workers.
Write-Host "=== Shutting down stale build servers ===" -ForegroundColor Cyan
dotnet build-server shutdown 2>$null | Out-Null

Write-Host "=== Packing to Local NuGet ===" -ForegroundColor Cyan
Write-Host "Target: $localNuget" -ForegroundColor Cyan

# Clean bin, obj, and .vs folders silently
$oldProgress = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'
@("bin", "obj", ".vs") | ForEach-Object {
    Get-ChildItem -Path $rootDir -Directory -Recurse -Filter $_ -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
} | Out-Null
$ProgressPreference = $oldProgress

# Ensure folder exists
New-Item -ItemType Directory -Force -Path $localNuget | Out-Null

# Read the version straight out of Directory.Build.props.
# Why: <VersionPrefix> is the single source of truth — the same property `dotnet pack` stamps.
# There is no tag inference and no MinVer, so detection and stamping cannot drift apart.
Write-Host "`n=== Detecting version ===" -ForegroundColor Cyan
$propsPath = Join-Path $PSScriptRoot '..' 'Directory.Build.props'
$prefixMatch = (Select-String -Path $propsPath -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' -ErrorAction SilentlyContinue | Select-Object -First 1)
$suffixMatch = (Select-String -Path $propsPath -Pattern '<VersionSuffix>([^<]+)</VersionSuffix>' -ErrorAction SilentlyContinue | Select-Object -First 1)
if ($prefixMatch) {
    $versionPrefix = $prefixMatch.Matches[0].Groups[1].Value.Trim()
}
if (-not $versionPrefix -or $versionPrefix -notmatch '^\d+\.\d+\.\d+') {
    Write-Error "Failed to read <VersionPrefix> from $propsPath. Got: '$versionPrefix'."
    exit 1
}
# Why: the package version is VersionPrefix + optional -VersionSuffix (1.0.0 + rc.1 => 1.0.0-rc.1),
# exactly what `dotnet pack` stamps. Compose both here so detection and stamping cannot drift.
$versionSuffix = if ($suffixMatch) { $suffixMatch.Matches[0].Groups[1].Value.Trim() } else { '' }
$version = if ($versionSuffix) { "$versionPrefix-$versionSuffix" } else { $versionPrefix }
Write-Host "Current version: $version" -ForegroundColor Green

# Delete old packages with this version from local folder (silently)
$oldProgress = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'
Get-ChildItem "$localNuget\Fdw.*.$version.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem "$localNuget\Fdw.*.$version.snupkg" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# Delete from NuGet cache (silently)
$cacheRoot = "$env:USERPROFILE\.nuget\packages"
if (Test-Path $cacheRoot) {
    Get-ChildItem $cacheRoot -Directory | Where-Object { $_.Name -like "fractaldataworks.*" } | ForEach-Object {
        $versionFolder = Join-Path $_.FullName $version
        if (Test-Path $versionFolder) {
            Remove-Item $versionFolder -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
$ProgressPreference = $oldProgress

# Create config file in the local folder
$nugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalFdw" value="%LocalNugetFolder%" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="LocalFdw">
      <!-- Why: every locally-produced ecosystem package prefix is routed to the
           local feed. Fdw.* also covers Fdw.Pidgin.*.
           Without the CyberdyneDevelopment.* entries, Mc3Po/DeveloperTools packages
           fall through to nuget.org and resolve stale/wrong versions on -Local builds. -->
      <package pattern="Fdw.*" />
      <package pattern="CyberdyneDevelopment.Mc3Po.*" />
      <package pattern="CyberdyneDevelopment.DeveloperTools.*" />
      <package pattern="Reference.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@
$nugetConfigPath = Join-Path $localNuget $ConfigName
Set-Content -Path $nugetConfigPath -Value $nugetConfigContent -Encoding UTF8

# Generate Tailwind safelist before build (matches pack-local.sh).
Write-Host "`n=== Generating Tailwind safelist ===" -ForegroundColor Cyan
& "$PSScriptRoot\generate-tailwind-safelist.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tailwind safelist generation failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$mainSln = Join-Path $rootDir "Fdw.DeveloperKit.slnx"

if ($NoBuild) {
    # Why: single slnx-level pack instead of a 75-project loop. The root
    # Directory.Build.props gates IsPackable=true only for src/* projects, so tests,
    # samples, vsix, and reference apps are skipped automatically. One MSBuild
    # bootstrap instead of 75 — drops the pack phase from minutes to seconds.
    Write-Host "`n=== Packing solution ($Configuration) [no-build, $mainSln] ===" -ForegroundColor Yellow
    dotnet pack $mainSln -c $Configuration -o $localNuget --no-build --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Pack failed"
        exit $LASTEXITCODE
    }
} else {
    if (-not (Test-Path $mainSln)) {
        Write-Error "Main solution not found at $mainSln"
        exit 1
    }

    Write-Host "`n=== Building solution ($Configuration) [$mainSln] ===" -ForegroundColor Cyan
    # Why: MSBuild file logger writes errors+warnings (with file:line detail) to disk
    # natively. Use a temp path so last-pack-errors.txt is only present if anything
    # was logged.
    $packTmpOut = [System.IO.Path]::GetTempFileName()
    $env:DOTNET_NOLOGO = "1"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    dotnet build $mainSln -c $Configuration --nologo `
        -fl "-flp:logfile=$packTmpOut;errorsonly;warningsonly;verbosity=normal"
    $buildRc = $LASTEXITCODE

    if ((Test-Path $packTmpOut) -and ((Get-Item $packTmpOut).Length -gt 0)) {
        Move-Item -Force $packTmpOut $packErrLog
        Write-Host "  wrote error/warning detail to $packErrLog" -ForegroundColor Yellow
    } else {
        Remove-Item $packTmpOut -Force -ErrorAction SilentlyContinue
    }

    if ($buildRc -ne 0) {
        Write-Error "Build failed (see $packErrLog if present)"
        exit $buildRc
    }

    # Why: single slnx-level pack instead of a 75-project loop. The root
    # Directory.Build.props gates IsPackable=true only for src/* projects, so tests,
    # samples, vsix, and reference apps are skipped automatically. One MSBuild
    # bootstrap instead of 75 — drops the pack phase from minutes to seconds.
    Write-Host "`n=== Packing solution ($Configuration) [no-build, $mainSln] ===" -ForegroundColor Cyan
    dotnet pack $mainSln -c $Configuration -o $localNuget --no-build --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Pack failed"
        exit $LASTEXITCODE
    }
}

# Generate Directory.Packages.props with all Fdw packages
$escapedVersion = [regex]::Escape($version)
$packages = Get-ChildItem "$localNuget\Fdw.*.$version.nupkg" |
    ForEach-Object { $_.BaseName -replace "\.$escapedVersion$", "" } |
    Sort-Object -Unique

$packageVersionLines = $packages | ForEach-Object {
    "    <PackageVersion Include=`"$_`" Version=`"$version`" />"
}

$directoryPackagesProps = @"
<Project>
  <!--
    Auto-generated by pack-local.ps1
    Contains Fdw package versions for local development.
    Consumer projects import this when using -Local configurations.
  -->
  <ItemGroup>
$($packageVersionLines -join "`n")
  </ItemGroup>
</Project>
"@
$propsPath = Join-Path $localNuget "Directory.Packages.props"
Set-Content -Path $propsPath -Value $directoryPackagesProps -Encoding UTF8

# Why: consumer projects' Directory.Packages.props imports FdwVersion.props from
# LocalNugetFolder when Configuration ends with -Local. That file pins the FdwVersion
# property — if it goes stale, every Debug-Local build resolves to an older pack
# even when local-nuget has newer nupkgs. Always rewrite it to match the current pack.
$fdwVersionProps = Join-Path $localNuget "FdwVersion.props"
Set-Content -Path $fdwVersionProps -Value "<Project><PropertyGroup><FdwVersion>$version</FdwVersion></PropertyGroup></Project>" -Encoding UTF8

# Update versions.json (preserve other entries)
$versionsPath = Join-Path $localNuget "versions.json"
$versions = @{}
if (Test-Path $versionsPath) {
    $existingContent = Get-Content $versionsPath -Raw
    if ($existingContent -and $existingContent.Trim() -ne "{}") {
        $versions = $existingContent | ConvertFrom-Json -AsHashtable
    }
}
$versions["Fdw"] = $version
$versions | ConvertTo-Json | Set-Content -Path $versionsPath -Encoding UTF8

Write-Host "`n=== Success ===" -ForegroundColor Green
Write-Host "$($packages.Count) packages (v$version) published to: $localNuget" -ForegroundColor Green
Write-Host "Run .\push-gitlab.sh to also publish to the GitLab feed." -ForegroundColor Cyan
