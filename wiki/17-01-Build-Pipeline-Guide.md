# Build Pipeline Guide

This guide covers the FractalDataWorks build pipeline: building and packing the framework into local NuGet packages so consumer projects (reference-api, reference-etl, reference-scheduler, reference-ui, customer apps) can resolve them.

## Repository Boundary

This repository (FractalDataWorks) ships only the framework. Reference applications and their build scripts live in their own repositories:

- `reference-api`
- `reference-etl`
- `reference-scheduler`
- `reference-ui`

Each reference repo pulls FDW packages from a NuGet feed (a published feed). They are built and deployed independently of this repo.

## Build Scripts

| Script | Purpose |
|--------|---------|
| `public/scripts/build-all.sh` | Pack FDW into the directory given with `-o` |

## build-all.sh

Builds and packs the framework. Two phases:

1. **Phase 1 — Pack FDW Framework.** Detects version via `nbgv get-version -v NuGetPackageVersion`, clears stale packages and cache entries, and runs `dotnet pack Fdw.DeveloperKit.slnx -c $CONFIGURATION -o $OUTPUT`.
2. **Phase 2 — Build and Test.** Runs the framework's tests against the freshly-built bits.

```bash
./public/scripts/build-all.sh -o <directory>            # Debug
./public/scripts/build-all.sh -o <directory> -c Release # Release (strict — zero warnings)
```

## Version Source of Truth

`version.json` at the repo root drives Nerdbank.GitVersioning. The version flows into:

- The package version embedded in each `FractalDataWorks.*.nupkg`


## Consumer-Side Build (Reference Repos)

Each reference repo has its own `Directory.Packages.props` centrally pinning the framework version. The version a build restores follows its tier (`FDW_TIER`: `internal`, `develop` or `release`; the pipeline states it from the branch): the first two float on their own tier's builds, `release` restores the hand-set `FdwReleaseVersion`.

Refer to each reference repo's own README / build scripts for its specific workflow.

## NuGet Cache Behaviour

A package re-built under a version already restored on a machine can be shadowed by the NuGet cache (the global cache is read before any feed), so a pack that must be seen again takes a new version, and a stale entry is cleared with `dotnet nuget locals all --clear`.

## Configurations

| Configuration | Use case |
|---------------|----------|
| `Debug` | Framework iteration, no analyzers |
| `Develop` | Framework with analyzers, warnings not errors |
| `Release` | Framework CI/CD, strict mode — zero warnings required |

## Troubleshooting

**"Could not detect FDW version"** — install Nerdbank.GitVersioning: `dotnet tool install -g nbgv`.

**Stale package version errors** — clear the NuGet cache:

```bash
rm -rf ~/.nuget/packages/fractaldataworks.*
```

Then restore again.

**Consumer restore fails with "package not found"** — check the version the consumer's tier resolves to (`FDW_TIER`, see 02-03 Package Management) is on the feed its `nuget.config` maps `Fdw.*` to.

## Related Documentation

- [02-02 Directory.Build.props](02-02-Directory-Build-Props.md) — MSBuild property hierarchy
- [02-03 Package Management](02-03-Package-Management.md) — central package versioning
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) — production deployment
