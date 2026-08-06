# ReferenceSolutions Multi-Solution Architecture

This guide explains the multi-solution enterprise architecture pattern demonstrated in the ReferenceSolutions sample, including package management for local development.

## Overview

ReferenceSolutions demonstrates how to structure a large enterprise application as multiple independent solutions that share common packages:

```
ReferenceSolutions/
├── SharedPackages/              # Shared internal packages
│   ├── SharedPackages.slnx
│   └── src/
│       ├── Reference.Data.Nfl/
│       ├── Reference.Authentication/
│       ├── Reference.Authentication.MultiTenant/
│       ├── Reference.Calculations.Contracts/
│       └── Reference.UI.Blazor/
│
├── ApiSolution/                 # REST API Server (port 5001)
│   ├── ApiSolution.slnx
│   ├── start-dev.ps1
│   └── src/Reference.Api/
│
├── EtlServer/                   # ETL Pipeline Server (port 5002)
│   ├── EtlServer.slnx
│   └── start-dev.ps1
│
├── SchedulerServer/             # Job Scheduling Server (port 5000)
│   ├── SchedulerServer.slnx
│   └── start-dev.ps1
│
├── ManagementUI/                # Blazor Management UI (port 5003)
│   ├── ManagementUI.slnx
│   ├── start-dev.ps1
│   └── src/Reference.Management.UI/
│
├── start.ps1                    # Interactive menu launcher
├── start-dev.ps1                # CLI launcher (api|etl|scheduler|ui)
├── start-all.ps1                # Start all services
├── pack-shared.ps1              # Publish SharedPackages
└── build-all.ps1                # Build everything
```

## Services Overview

| Service | Port | Purpose |
|---------|------|---------|
| **Reference.Api** | 5001 | REST API with FastEndpoints, Connections, DataStores, DataSets, Schemas, Calculations |
| **Reference.Etl.Server** | 5003 | ETL pipeline execution server |
| **Reference.Scheduler.Server** | 5005 | Job scheduling with cron/interval triggers |
| **Reference.Management.UI** | 5007 | Blazor Server UI for managing configurations |

## Database Setup

The Reference Solution uses SQL Server with a dacpac-based deployment approach.

### Quick Start

```powershell
cd samples/ReferenceSolutions/ApiSolution

# Build the dacpac from sqlproj
./docker/mssql/build-dacpac.ps1

# Start SQL Server with schema deployment
docker-compose -f docker-compose.dacpac.yml up -d
```

### Database Schema

**ConfigurationDb** contains 50 tables across 5 schemas:

| Schema | Tables | Purpose |
|--------|--------|---------|
| **cfg** | 34 | Configuration (connections, datasets, auth, tenants) |
| **etl** | 5 | ETL pipelines and execution history |
| **sched** | 5 | Scheduling (cron, interval) |
| **ops** | 3 | Operations (escalation, workflows) |
| **types** | 3 | TypeCollection metadata persistence |

### Schema Source

The canonical schema is defined in `public/databases/ControlDb.sqlproj` (SQL Server Database Project using Microsoft.Build.Sql SDK 2.0.0).

For detailed table documentation, see [docker/mssql/README.md](../samples/ReferenceSolutions/ApiSolution/docker/mssql/README.md).

## Service Bootstrap Pattern

All Reference Solutions use the three-phase registration pattern for service domains:

```csharp
// Program.cs - Simplified bootstrap sequence

// Phase 1: Configure (bind IOptions) & Register (add factories)
SecretManagerTypes.Configure(builder.Services, builder.Configuration, loggerFactory);
SecretManagerTypes.Register(builder.Services, loggerFactory);

ConnectionTypes.Configure(builder.Services, builder.Configuration, loggerFactory);
ConnectionTypes.Register(builder.Services, loggerFactory);
ConnectionTypes.RegisterAdditionalInterfaces(builder.Services);  // IDataConnectionProvider

DataStoreServiceTypes.Configure(builder.Services, builder.Configuration, loggerFactory);
DataStoreServiceTypes.Register(builder.Services, loggerFactory);

// Additional providers (DataSets, DataGateway)
builder.Services.AddDefaultDataStoreProvider(builder.Configuration);
builder.Services.AddSingleton<IDataSetSourceResolver>(sp =>
    new DataSetSourceResolver(bootstrapConnection, logger));
builder.Services.AddDataGateway();

var app = builder.Build();

// Phase 2: Initialize (eager resolution, fail-fast)
SecretManagerTypes.Initialize(app.Services, loggerFactory);
ConnectionTypes.Initialize(app.Services, loggerFactory);
DataStoreServiceTypes.Initialize(app.Services, loggerFactory);
```

**Registration Order:** SecretManagers → Connections → DataStores → Domain-Specific (ETL, Scheduler)

## Package Management

ReferenceSolutions uses a **unified package source** approach - all packages (FractalDataWorks core and SharedPackages) are published to `LocalNugetFolder`:

| Package Prefix | Source | Purpose |
|----------------|--------|---------|
| `FractalDataWorks.*` | `LocalNugetFolder` | Core framework from FDW repo |
| `Reference.*` | Project Reference | SharedPackages between solutions |

### SharedPackages Naming

SharedPackages use the `Reference.*` prefix (simplified from `FractalDataWorks.Reference.*`):

| Package | Purpose |
|---------|---------|
| `Reference.Data.Nfl` | NFL sports data models and datasets |
| `Reference.Authentication` | SQL-based authentication service |
| `Reference.Authentication.MultiTenant` | Multi-tenant authentication extensions |
| `Reference.Calculations.Contracts` | Shared calculation request/response models |
| `Reference.UI.Blazor` | Shared Blazor UI components and themes |

These are referenced as **project references** within the ReferenceSolutions folder, not as NuGet packages.

## Package Flow

```
FractalDataWorks                 SharedPackages
(scripts/pack-local.ps1)         (pack-shared.ps1)
        │                              │
        └──────────┬───────────────────┘
                   │
                   ▼
            LocalNugetFolder
            (FractalDataWorks.* pattern)
                   │
                   ▼
            Consumer Solutions
        (ApiSolution, EtlServer, etc.)
```

## Setting Up

### 1. One-Time Setup

Set up FractalDataWorks local NuGet folder:

```powershell
# From FractalDataWorks repo
cd public
./scripts/setup-local-nuget.ps1 -Path C:\development\local-nuget

# Pack FractalDataWorks packages
./scripts/pack-local.ps1
```

This creates:
- `C:\development\local-nuget\` with FDW packages
- `Fdw.Local.nuget.config` with source mapping
- Sets `LocalNugetFolder` environment variable

### 2. Pack SharedPackages

Pack SharedPackages to LocalNugetFolder:

```powershell
cd samples/ReferenceSolutions
./pack-shared.ps1
```

This:
- Cleans old `FractalDataWorks.Reference.*` packages from LocalNugetFolder
- Clears NuGet cache for those packages
- Packs to `$LocalNugetFolder` using nbgv for versioning

## pack-shared.ps1 Explained

The `pack-shared.ps1` script:

1. **Validates** `LocalNugetFolder` environment variable is set
2. **Gets version** from Nerdbank.GitVersioning (same as FDW packages)
3. **Cleans old packages** from `LocalNugetFolder` matching `FractalDataWorks.Reference.*`
4. **Clears NuGet cache** for those packages
5. **Packs entire solution** to `LocalNugetFolder`

```powershell
# Default usage
./pack-shared.ps1

# Skip build (if already built)
./pack-shared.ps1 -NoBuild
```

### Adding New SharedPackages

1. Create project in `SharedPackages/src/`:
   ```powershell
   cd SharedPackages/src
   dotnet new classlib -n FractalDataWorks.Reference.NewPackage
   ```

2. Add to solution:
   ```powershell
   cd SharedPackages
   dotnet sln add src/FractalDataWorks.Reference.NewPackage/FractalDataWorks.Reference.NewPackage.csproj
   ```

3. Configure for packaging in `.csproj`:
   ```xml
   <PropertyGroup>
     <IsPackable>true</IsPackable>
     <PackageId>FractalDataWorks.Reference.NewPackage</PackageId>
   </PropertyGroup>
   ```

4. Pack:
   ```powershell
   cd ReferenceSolutions
   ./pack-shared.ps1
   ```

5. Reference in consumer solutions' `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="FractalDataWorks.Reference.NewPackage" Version="$(FdwVersion)" />
   ```

## nuget.config Structure

Each solution has a simple `nuget.config` with two sources:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="LocalFdw" value="%LocalNugetFolder%" />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="LocalFdw">
      <package pattern="FractalDataWorks.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Directory.Packages.props Structure

Each solution uses Central Package Management with a single version variable:

```xml
<Project>
  <PropertyGroup>
    <!-- FractalDataWorks version - all packages share this -->
    <FdwVersion>0.1.0-preview.3.1234</FdwVersion>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- FractalDataWorks Core -->
    <PackageVersion Include="FractalDataWorks.Abstractions" Version="$(FdwVersion)" />
    <PackageVersion Include="FractalDataWorks.Collections" Version="$(FdwVersion)" />
    <!-- ... more FDW packages ... -->
  </ItemGroup>

  <ItemGroup>
    <!-- SharedPackages (also from LocalNugetFolder) -->
    <PackageVersion Include="FractalDataWorks.Reference.Data.Nfl" Version="$(FdwVersion)" />
    <PackageVersion Include="FractalDataWorks.Reference.Authentication" Version="$(FdwVersion)" />
    <PackageVersion Include="FractalDataWorks.Reference.Authentication.MultiTenant" Version="$(FdwVersion)" />
  </ItemGroup>

  <ItemGroup>
    <!-- External: Third-party packages -->
    <PackageVersion Include="FastEndpoints" Version="7.2.0-beta.10" />
    <!-- ... more external packages ... -->
  </ItemGroup>
</Project>
```

## Starting Solutions

### Interactive Menu

```powershell
./start.ps1
```

Opens an interactive menu with options to:
- Start any solution (ApiSolution, EtlServer, SchedulerServer)
- Pack SharedPackages
- Build all solutions
- Manage Docker containers
- Toggle LocalBuild mode

### Command Line

```powershell
./start-dev.ps1 api                # Start ApiSolution
./start-dev.ps1 etl                # Start EtlServer
./start-dev.ps1 scheduler          # Start SchedulerServer

# With build options
./start-dev.ps1 api -LocalBuild    # Rebuild with local FDW packages
./start-dev.ps1 etl -Build         # Rebuild with NuGet packages
```

### start-dev.ps1 Options

Each solution also has its own `start-dev.ps1` script:

| Flag | Configuration | NuGet Source | Use Case |
|------|---------------|--------------|----------|
| (none) | Release-Local | Cached | Just run, no rebuild |
| `-Build` | Release | NuGet.org | Clean build with published packages |
| `-LocalBuild` | Release-Local | LocalNugetFolder | Clean build with local FDW packages |

### -LocalBuild Mode

When you're developing FractalDataWorks alongside your solution:

```powershell
# 1. Pack latest FDW changes
cd FractalDataWorks/public
./scripts/pack-local.ps1

# 2. Pack SharedPackages (if changed)
cd samples/ReferenceSolutions
./pack-shared.ps1

# 3. Build and run with local packages
./start-dev.ps1 api -LocalBuild
```

### -Build Mode

When using published/stable packages:

```powershell
./start-dev.ps1 api -Build
```

## Build Configuration Details

The `-Local` suffix configurations work through MSBuild:

### Directory.Build.props (in each solution)

```xml
<!-- Use local NuGet config when LocalNugetFolder is set -->
<PropertyGroup Condition="'$(Configuration.EndsWith(`-Local`))' AND '$(LocalNugetFolder)' != ''">
  <RestoreConfigFile>$(LocalNugetFolder)\Fdw.Local.nuget.config</RestoreConfigFile>
</PropertyGroup>
```

### Available Configurations

| Configuration | Analyzers | Local Packages | Use |
|---------------|-----------|----------------|-----|
| `Debug` | No | From cache | Fast iteration |
| `Debug-Local` | No | From LocalNugetFolder | Test local FDW |
| `Release` | Yes | From NuGet | Production |
| `Release-Local` | Yes | From LocalNugetFolder | Test before publish |

## build-all.ps1 Options

The master build script:

```powershell
# Build everything (FDW + SharedPackages + all solutions)
./build-all.ps1

# Skip FDW, just build ReferenceSolutions
./build-all.ps1 -SkipFdw

# Build only a specific solution
./build-all.ps1 -Solution Api
```

## Workflow Examples

### Daily Development

```powershell
# Start working on ApiSolution
cd samples/ReferenceSolutions
./start-dev.ps1 api
```

### After Changing SharedPackages

```powershell
# Re-pack shared packages
./pack-shared.ps1

# Restart solution (will pick up new packages)
./start-dev.ps1 api -LocalBuild
```

### After Changing FractalDataWorks

```powershell
# 1. Pack FDW
cd FractalDataWorks/public
./scripts/pack-local.ps1

# 2. Rebuild solution with local packages
cd samples/ReferenceSolutions
./start-dev.ps1 api -LocalBuild
```

### Full Clean Build

```powershell
# Clean everything and rebuild
./build-all.ps1
```

## External Projects (Outside FractalDataWorks)

If you're building a project that depends on FractalDataWorks but is **not** inside the `FractalDataWorks` repository, follow these steps:

### Option 1: Local Development with LocalNugetFolder

This approach uses the same local package workflow:

1. **Set up LocalNugetFolder** (one-time):
   ```powershell
   # From FractalDataWorks repo
   cd FractalDataWorks/public
   ./scripts/setup-local-nuget.ps1 -Path C:\development\local-nuget
   ```

2. **Pack FractalDataWorks packages**:
   ```powershell
   cd FractalDataWorks/public
   ./scripts/pack-local.ps1
   ```

3. **Configure your project's nuget.config**:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="LocalFdw" value="%LocalNugetFolder%" />
     </packageSources>

     <packageSourceMapping>
       <packageSource key="LocalFdw">
         <package pattern="FractalDataWorks.*" />
       </packageSource>
       <packageSource key="nuget.org">
         <package pattern="*" />
       </packageSource>
     </packageSourceMapping>
   </configuration>
   ```

4. **Create Directory.Packages.props** (if using Central Package Management):
   ```xml
   <Project>
     <PropertyGroup>
       <FdwVersion>0.1.0-preview.3.1234</FdwVersion>
       <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
     </PropertyGroup>

     <ItemGroup>
       <PackageVersion Include="FractalDataWorks.Abstractions" Version="$(FdwVersion)" />
       <!-- Add other FDW packages as needed -->
     </ItemGroup>
   </Project>
   ```

5. **Build with Release-Local configuration**:
   ```powershell
   dotnet build -c Release-Local
   ```

### Option 2: Using Fdw.Local.nuget.config

The `pack-local.ps1` script creates `Fdw.Local.nuget.config` in LocalNugetFolder. You can reference it directly:

```xml
<!-- In Directory.Build.props -->
<PropertyGroup Condition="'$(Configuration.EndsWith(`-Local`))' AND '$(LocalNugetFolder)' != ''">
  <RestoreConfigFile>$(LocalNugetFolder)\Fdw.Local.nuget.config</RestoreConfigFile>
</PropertyGroup>
```

This makes `Release-Local` and `Debug-Local` configurations automatically use LocalNugetFolder.

### Option 3: Published NuGet Feed

For production and CI/CD, FractalDataWorks packages should be published to a private NuGet feed:

1. **Configure nuget.config** with your private feed:
   ```xml
   <packageSources>
     <clear />
     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     <add key="FdwFeed" value="https://your-org.pkgs.visualstudio.com/_packaging/FractalDataWorks/nuget/v3/index.json" />
   </packageSources>

   <packageSourceMapping>
     <packageSource key="FdwFeed">
       <package pattern="FractalDataWorks.*" />
     </packageSource>
     <packageSource key="nuget.org">
       <package pattern="*" />
     </packageSource>
   </packageSourceMapping>
   ```

2. **Authenticate** (for Azure Artifacts):
   ```powershell
   dotnet nuget add source "https://your-org.pkgs.visualstudio.com/_packaging/FractalDataWorks/nuget/v3/index.json" --name FdwFeed --username your-username --password your-pat
   ```

### Recommended Project Structure for External Projects

```
YourProject/
├── YourProject.slnx
├── nuget.config                  # Package source configuration
├── Directory.Build.props         # Common build settings
├── Directory.Packages.props      # Central Package Management
└── src/
    └── YourProject.Api/
        └── YourProject.Api.csproj
```

### Version Synchronization

When developing against local FDW packages:

1. **Get the current version** from FDW:
   ```powershell
   cd FractalDataWorks/public
   nbgv get-version -v NuGetPackageVersion
   ```

2. **Update your Directory.Packages.props** with that version:
   ```xml
   <FdwVersion>0.1.0-preview.3.1234</FdwVersion>
   ```

3. **After packing new FDW packages**, increment the version number in your project to match.

## CI/CD Considerations

For CI/CD pipelines:

1. **FractalDataWorks packages** should be published to a private NuGet feed (Azure Artifacts, GitHub Packages, etc.)
2. **SharedPackages** (if used) should also be published to the same or separate feed
3. **Consumer projects** reference published versions, not local development versions
4. **Version coordination** happens through `Directory.Packages.props` updates

Example CI workflow:

```yaml
# .github/workflows/build.yml
- name: Build with local packages
  env:
    LocalNugetFolder: ${{ github.workspace }}/local-nuget
  run: |
    # Restore using local config
    dotnet restore --configfile nuget.config
    dotnet build -c Release-Local
```

## Related Documentation

- [Local Package Development](01-04-Local-Package-Development.md) - FDW local package workflow
- [Package Management](02-03-Package-Management.md) - Central Package Management
- [Building an Authentication Service](10-02-Building-Authentication-Service.md) - Example SharedPackage

## Demo Credentials Banner

The Reference.Api includes a **demo credentials banner** at the top of the Scalar UI documentation page. This banner displays the test usernames and passwords for quick reference during development.

**Banner Display:**
- Fixed position at the top of the Scalar UI
- Shows: `testuser / TestPassword123!` (User role) and `admin / AdminPassword123!` (Admin role)
- Red "Demo Only - Do Not Use in Production" warning badge

> ⚠️ **WARNING:** This banner is for demonstration purposes only. **Do not do this in a real solution.** Never expose credentials in production applications.

The banner is implemented by injecting CSS and JavaScript into the Scalar API reference configuration in `Program.cs`:

```csharp
app.MapScalarApiReference(options =>
{
    options.AddHeadContent($@"
        <style>
            /* DEMO CREDENTIALS BANNER - DO NOT DO THIS IN A REAL SOLUTION */
            .demo-credentials {{ ... }}
        </style>
        <script>
            // Inject demo credentials banner - DO NOT DO THIS IN A REAL SOLUTION
            document.addEventListener('DOMContentLoaded', function() {{
                const banner = document.createElement('div');
                banner.className = 'demo-credentials';
                // ...
            }});
        </script>
    ");
});
```

