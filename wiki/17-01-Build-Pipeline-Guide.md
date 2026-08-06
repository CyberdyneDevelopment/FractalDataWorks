# Build Pipeline Guide

This guide covers the FractalDataWorks build pipeline: building and packing the framework into local NuGet packages so consumer projects (reference-api, reference-etl, reference-scheduler, reference-ui, customer apps) can resolve them.

## Repository Boundary

This repository (FractalDataWorks) ships only the framework. Reference applications and their build scripts live in their own repositories:

- `reference-api`
- `reference-etl`
- `reference-scheduler`
- `reference-ui`

Each reference repo pulls FDW packages from a NuGet feed (local or a published feed). They are built and deployed independently of this repo.

## Build Scripts

| Script | Purpose |
|--------|---------|
| `public/scripts/pack-local.sh` | Build + pack FDW into `$LocalNugetFolder` (default Release) |
| `public/scripts/build-all.sh` | Two-phase build: pack FDW, then build/test the framework |
| `public/scripts/setup-local-nuget.sh` | Initializes `LocalNugetFolder` and writes `Fdw.Local.nuget.config` |

## pack-local.sh

Standalone pack workflow. Cleans stale build servers, deletes old `FractalDataWorks.*.nupkg` from `$LocalNugetFolder` and `~/.nuget/packages`, then runs `dotnet pack` on `Fdw.DeveloperKit.slnx`.

```bash
./public/scripts/pack-local.sh                        # Build + pack (Release)
./public/scripts/pack-local.sh --no-build             # Pack only (reuse existing build output)
./public/scripts/pack-local.sh -c Debug               # Use Debug configuration
```

Prerequisites:
- `LocalNugetFolder` environment variable set (e.g., `~/development/local-nuget`)
- Nerdbank.GitVersioning (`nbgv`) installed for deterministic versioning

## build-all.sh

Builds and packs the framework. Two phases:

1. **Phase 1 — Pack FDW Framework.** Detects version via `nbgv get-version -v NuGetPackageVersion`, clears stale packages and cache entries, and runs `dotnet pack Fdw.DeveloperKit.slnx -c $CONFIGURATION -o $LocalNugetFolder`.
2. **Phase 2 — Build and Test.** Runs the framework's tests against the freshly-built bits.

```bash
./public/scripts/build-all.sh                  # Debug
./public/scripts/build-all.sh -c Release       # Release (strict — zero warnings)
```

## Version Source of Truth

`version.json` at the repo root drives Nerdbank.GitVersioning. The version flows into:

- The package version embedded in each `FractalDataWorks.*.nupkg`
- `FdwVersion.props` and `versions.json` written into `$LocalNugetFolder`

Consumer repos read `FdwVersion.props` (when using `-Local` build configurations) to pin their `Directory.Packages.props` `<FdwVersion>` against the freshly packed framework.

## Consumer-Side Build (Reference Repos)

Each reference repo has its own `Directory.Packages.props` with `<FdwVersion>X.Y.Z</FdwVersion>` centrally pinning the framework version. The `-Local` build configurations import `LocalNugetFolder/FdwVersion.props` to override `<FdwVersion>` with the locally-packed version, so a freshly built framework propagates without editing the consumer's props file.

Refer to each reference repo's own README / build scripts for its specific workflow.

## Local NuGet Configuration

`pack-local.sh` writes `Fdw.Local.nuget.config` into `$LocalNugetFolder`:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="LocalFdw" value="%LocalNugetFolder%" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
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

`FractalDataWorks.*` packages resolve from the local folder; everything else resolves from nuget.org.

## NuGet Cache Behaviour

Local builds may produce the same version number across runs (same git height = same nbgv version), so the NuGet cache can shadow a fresh package. Both `pack-local.sh` and `build-all.sh` aggressively delete `~/.nuget/packages/fractaldataworks.*` and old `FractalDataWorks.*.nupkg` from `$LocalNugetFolder` before packing. Consumer projects must restore with `--force` after a re-pack.

## Configurations

| Configuration | Use case |
|---------------|----------|
| `Debug` | Framework iteration, no analyzers |
| `Develop` | Framework with analyzers, warnings not errors |
| `Release` | Framework CI/CD, strict mode — zero warnings required |
| `Debug-Local` | Consumer repos pulling FDW from the local feed |
| `Release-Local` | Consumer repos, local feed, strict mode |

## Troubleshooting

**"LocalNugetFolder environment variable not set"** — run `source public/scripts/setup-local-nuget.sh` or export the variable manually.

**"Could not detect FDW version"** — install Nerdbank.GitVersioning: `dotnet tool install -g nbgv`.

**Stale package version errors** — clear the NuGet cache:

```bash
rm -rf ~/.nuget/packages/fractaldataworks.*
rm -f "$LocalNugetFolder"/FractalDataWorks.*.nupkg
```

Then re-run `pack-local.sh`.

**Consumer restore fails with "package not found"** — verify `LocalNugetFolder` contains `.nupkg` files for the expected version, `Fdw.Local.nuget.config` exists in `LocalNugetFolder`, and the consumer's restore uses the local config (`dotnet restore --configfile $LocalNugetFolder/Fdw.Local.nuget.config --force`).

## Related Documentation

- [01-04 Local Package Development](01-04-Local-Package-Development.md) — setting up `LocalNugetFolder`
- [02-02 Directory.Build.props](02-02-Directory-Build-Props.md) — MSBuild property hierarchy
- [02-03 Package Management](02-03-Package-Management.md) — central package versioning
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) — production deployment
