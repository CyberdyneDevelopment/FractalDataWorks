# Local Package Development

This guide explains how to set up and use local NuGet packages for development and testing. This workflow is useful for:

- **FractalDataWorks Contributors**: Testing changes before publishing
- **Application Developers**: Testing pre-release FractalDataWorks packages
- **Framework Developers**: Building frameworks that depend on FractalDataWorks packages

## Overview

The local package development workflow allows you to:
1. Pack packages to a local folder instead of publishing to nuget.org
2. Automatically use local packages when building any `*-Local` configuration (e.g. `Debug-Local`, `Release-Local`)
3. Seamlessly switch to published packages for non-`-Local` builds
4. Clean old package versions from local cache
5. Customize config names for different workflows

## Quick Start

### 1. Setup (One-Time)

**PowerShell:**
```powershell
cd public
.\scripts\setup-local-nuget.ps1
```

**Bash/Linux/Mac:**
```bash
cd public
./scripts/setup-local-nuget.sh
```

This creates:
- Local NuGet folder (default: `C:\development\local-nuget` or `~/development/local-nuget`)
- NuGet config file with source mapping (`Fdw.Local.nuget.config`)

### 2. Set Environment Variable

**PowerShell (Current Session):**
```powershell
$env:LocalNugetFolder = "C:\development\local-nuget"
```

**PowerShell (Permanent - User Level):**
```powershell
[System.Environment]::SetEnvironmentVariable('LocalNugetFolder', 'C:\development\local-nuget', 'User')
# Restart terminal/IDE for changes to take effect
```

**PowerShell Profile (Automatic on startup):**
```powershell
notepad $PROFILE
# Add this line:
$env:LocalNugetFolder = "C:\development\local-nuget"
```

**Bash (Current Session):**
```bash
export LocalNugetFolder="$HOME/development/local-nuget"
```

**Bash (Permanent):**
```bash
echo 'export LocalNugetFolder="$HOME/development/local-nuget"' >> ~/.bashrc
source ~/.bashrc
```

### 3. Pack and Test

**PowerShell:**
```powershell
cd public
.\scripts\pack-local.ps1
```

**Bash:**
```bash
cd public
./scripts/pack-local.sh
```

This will:
1. Read the hand-set version from `Directory.Build.props` (`VersionPrefix`/`VersionSuffix`)
2. Delete old packages with same version from local folder
3. Clear matching versions from NuGet cache (`~/.nuget/packages`)
4. Generate/update `Fdw.Local.nuget.config` with source mapping
5. Pack all Fdw.* packages to local folder

### 4. Build Your Projects

Projects configured with the local NuGet setup will automatically use local packages when:
- Building in a configuration whose name ends in `-Local` (for example `Debug-Local` or `Release-Local`)
- `LocalNugetFolder` environment variable is set

Configurations without the `-Local` suffix always use the standard NuGet config (published packages).

## Advanced Configuration

### Custom Config Names

You can customize the NuGet config filename for different workflows (e.g., separate configs for different framework versions):

**Setup with custom name:**
```powershell
# PowerShell
.\scripts\setup-local-nuget.ps1 -Path C:\dev\fdw-local -ConfigName MyProject.nuget.config

# Bash
./scripts/setup-local-nuget.sh ~/dev/fdw-local MyProject.nuget.config
```

**Pack with custom name:**
```powershell
# PowerShell
.\scripts\pack-local.ps1 -ConfigName MyProject.nuget.config

# Bash
./scripts/pack-local.sh MyProject.nuget.config
```

**Configure in your project:**

Add to your project's `Directory.Build.props`:
```xml
<PropertyGroup>
  <LocalNugetConfigFileName>MyProject.nuget.config</LocalNugetConfigFileName>
</PropertyGroup>
```

### Multiple Local Folders

You can maintain separate local NuGet folders for different purposes:

```powershell
# Setup multiple folders
.\scripts\setup-local-nuget.ps1 C:\dev\fdw-stable -ConfigName Stable.nuget.config
.\scripts\setup-local-nuget.ps1 C:\dev\fdw-experimental -ConfigName Experimental.nuget.config

# Switch between them by changing environment variable
$env:LocalNugetFolder = "C:\dev\fdw-stable"
$env:LocalNugetConfigFileName = "Stable.nuget.config"

# Or
$env:LocalNugetFolder = "C:\dev\fdw-experimental"
$env:LocalNugetConfigFileName = "Experimental.nuget.config"
```

### Custom Folder Location

**PowerShell:**
```powershell
.\scripts\setup-local-nuget.ps1 -Path D:\Projects\NuGetLocal
$env:LocalNugetFolder = "D:\Projects\NuGetLocal"
```

**Bash:**
```bash
./scripts/setup-local-nuget.sh /mnt/data/nuget-local
export LocalNugetFolder="/mnt/data/nuget-local"
```

## How It Works

### Package Source Mapping

The generated `Fdw.Local.nuget.config` uses NuGet's [package source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping) feature:

```xml
<packageSourceMapping>
  <packageSource key="LocalFdw">
    <package pattern="FractalDataWorks.*" />
  </packageSource>
  <packageSource key="nuget.org">
    <package pattern="*" />
  </packageSource>
</packageSourceMapping>
```

This ensures:
- All `FractalDataWorks.*` packages come from your local folder
- All other packages come from nuget.org
- No ambiguity about package sources

### Conditional Config Selection

Projects can use `Directory.Build.props` to conditionally use the local config.

From [`public/Directory.Build.props:184-193`](../Directory.Build.props#L184-L193):

```xml
<!-- Local NuGet Development Configuration -->
<PropertyGroup>
  <!-- Default config filename - can be overridden in project files -->
  <LocalNugetConfigFileName Condition="'$(LocalNugetConfigFileName)' == ''">Fdw.Local.nuget.config</LocalNugetConfigFileName>
</PropertyGroup>

<!-- Use local NuGet config when using a -Local configuration and LocalNugetFolder is set -->
<PropertyGroup Condition="'$(IsLocalConfiguration)' == 'true' AND '$(LocalNugetFolder)' != ''">
  <RestoreConfigFile>$(LocalNugetFolder)\$(LocalNugetConfigFileName)</RestoreConfigFile>
</PropertyGroup>
```

`IsLocalConfiguration` is set to `true` when `$(Configuration)` ends with `-Local` (see `public/Directory.Build.props:16-17`).

This pattern allows:
- **Local Development**: `*-Local` configurations use local packages
- **CI/CD**: Non-`-Local` configurations use published packages from nuget.org
- **Flexibility**: Projects can override `LocalNugetConfigFileName` for custom workflows

### Version Detection and Cleanup

The pack script reads the hand-set version from `Directory.Build.props`
(`VersionPrefix` + `VersionSuffix`, e.g. `1.0.0-rc.1`) — git tags and commit distance play no part.

It then:
1. Finds packages in local folder matching: `Fdw.*.{version}.nupkg`
2. Deletes them to prevent stale packages
3. Scans `~/.nuget/packages/fdw.*/{version}/` and removes cached versions
4. Packs fresh packages with current version

This ensures you're always testing the latest local changes.

## Troubleshooting

### Packages Not Using Local Version

**Check environment variable is set:**
```powershell
# PowerShell
echo $env:LocalNugetFolder

# Bash
echo $LocalNugetFolder
```

**Verify build configuration:**
- Local config only applies to `*-Local` configurations (e.g. `Debug-Local`)
- Non-`-Local` builds intentionally use nuget.org

**Check config file exists:**
```powershell
# PowerShell
Test-Path "$env:LocalNugetFolder\Fdw.Local.nuget.config"

# Bash
ls "$LocalNugetFolder/Fdw.Local.nuget.config"
```

**Force restore:**
```bash
dotnet restore --force
dotnet build
```

### Version Conflicts

If you're still getting old versions:

1. **Clear all NuGet caches:**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Delete bin/obj folders:**
   ```bash
   # PowerShell
   Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force

   # Bash
   find . -type d -name "bin" -o -name "obj" | xargs rm -rf
   ```

3. **Run pack-local script again:**
   ```bash
   .\scripts\pack-local.ps1
   ```

### IDE Not Picking Up Changes

**Visual Studio / Rider:**
- Close all instances
- Delete `.vs` folder (Visual Studio) or `.idea` folder (Rider)
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Reopen solution

**VS Code:**
- Reload window (Ctrl+Shift+P → "Reload Window")
- Or restart VS Code

## Best Practices

### For Framework Developers

1. **Set up once per machine:**
   ```bash
   .\scripts\setup-local-nuget.ps1
   ```

2. **Add to PowerShell profile for automatic setup:**
   ```powershell
   $env:LocalNugetFolder = "C:\development\local-nuget"
   ```

3. **Use pack-local before testing changes:**
   ```bash
   # Make changes to FractalDataWorks code
   .\scripts\pack-local.ps1

   # Build your test project
   cd samples/ReferenceSolution
   dotnet build
   ```

4. **Test in Debug, release with Release config:**
   ```bash
   # Test with local packages
   dotnet build -c Debug

   # Verify published packages work
   dotnet build -c Release
   ```

### For Application Developers

1. **Create project-specific config:**
   ```powershell
   .\scripts\setup-local-nuget.ps1 `
     -Path C:\MyApp\local-packages `
     -ConfigName MyApp.nuget.config
   ```

2. **Add to your project's Directory.Build.props:**
   ```xml
   <PropertyGroup>
     <LocalNugetConfigFileName>MyApp.nuget.config</LocalNugetConfigFileName>
     <IsLocalConfiguration>false</IsLocalConfiguration>
     <IsLocalConfiguration Condition="$(Configuration.EndsWith('-Local'))">true</IsLocalConfiguration>
   </PropertyGroup>

   <PropertyGroup Condition="'$(IsLocalConfiguration)' == 'true' AND '$(LocalNugetFolder)' != ''">
     <RestoreConfigFile>$(LocalNugetFolder)\$(LocalNugetConfigFileName)</RestoreConfigFile>
   </PropertyGroup>
   ```

3. **Set environment variable per project:**
   ```powershell
   # In your project's root folder
   $env:LocalNugetFolder = "C:\MyApp\local-packages"
   ```

4. **Copy FractalDataWorks packages from their local folder:**
   ```powershell
   # After FractalDataWorks team runs pack-local
   Copy-Item "C:\development\local-nuget\FractalDataWorks.*.nupkg" `
             "C:\MyApp\local-packages\"
   ```

### Team Workflows

**Scenario: Testing a contributor's branch**

1. Contributor packs their changes:
   ```bash
   git checkout feature/new-feature
   .\scripts\pack-local.ps1
   ```

2. Share the package version with team:
   ```bash
   # Version is automatically detected and displayed
   # e.g., "0.25.0-alpha.1234.gabcd123"
   ```

3. Team members copy packages:
   ```bash
   $contributorPkg = "\\shared\nuget\contributor-feature-branch"
   Copy-Item "$contributorPkg\FractalDataWorks.*.nupkg" "$env:LocalNugetFolder\"
   ```

4. Test with those packages:
   ```bash
   dotnet restore --force
   dotnet build -c Debug
   ```

## CI/CD Considerations

The local package development workflow is designed to be **CI/CD safe**:

- **Local Config Ignored**: Local config files (in `LocalNugetFolder`) are never committed to the repository
- **Non-`-Local` Builds**: CI/CD typically builds without the `-Local` suffix, which ignores `LocalNugetFolder`
- **No Environment Variable**: CI/CD agents don't have `LocalNugetFolder` set, so they use standard `public/nuget.config`
- **Predictable Behavior**: Published packages always come from nuget.org in CI/CD

To explicitly test with published packages locally:
```bash
dotnet build -c Release
```

## Related Documentation

- [Package Management](02-03-Package-Management.md) - Central Package Management
- [Directory.Build.props](02-02-Directory-Build-Props.md) - Build configuration
- [Quick Start](01-02-Quick-Start.md) - General getting started guide
