# Directory.Build.props

`Directory.Build.props` provides shared MSBuild properties for every project in the FractalDataWorks repository. The authoritative file lives at [`public/Directory.Build.props`](../Directory.Build.props) and is much richer than the per-sample one.

## Key Settings (from `public/Directory.Build.props`)

### Language & framework

```xml
<LangVersion>preview</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>disable</ImplicitUsings>
```

`ImplicitUsings` is intentionally disabled — all `using` directives are explicit.

### Configurations

```xml
<Configurations>Debug;Develop;Release;Test</Configurations>
```

The conditions that switch analyzers, defines and warning levels read `$(Configuration)` directly. The `-Local` configurations (a local NuGet folder feed) are retired: packages come from the published feeds, and the version a consumer restores follows its tier.

### Central Package Management

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
<ManagePackageVersionsCentrally Condition="$(MSBuildProjectDirectory.Contains('/samples/')) OR $(MSBuildProjectDirectory.Contains('\samples\'))">false</ManagePackageVersionsCentrally>
```

Samples opt out of CPM so they can pin to specific published package versions.

### Packaging defaults

```xml
<IsPackable>false</IsPackable>
<IsPackable Condition="$(MSBuildProjectDirectory.Contains('/src/')) OR $(MSBuildProjectDirectory.Contains('\src\'))">true</IsPackable>
```

Only projects under `src/` are packable by default.

### Source Link and symbols

```xml
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
```

Source Link is disabled outside CI to avoid local "no source control info" warnings:

```xml
<WarnOnNonCIBuild Condition="'$(CI)' != 'true'">false</WarnOnNonCIBuild>
<EnableSourceLink Condition="'$(CI)' != 'true'">false</EnableSourceLink>
```

### Versioning

A package's version is not typed in a project file. MinVer and tag-driven stamping are removed, and since the per-package
versioning (after 1.5.5) a release gives each package that changed its own version: the next patch, minor or major from its own
last stable version, at the lowest level its public API change needs (decided from the built public surface compared with the last
stable package). `Directory.Build.props` declares only the line, `major.minor`, which development builds and consumers float on:

```xml
<VersionPrefix>1.5.1</VersionPrefix>
```

Branching, building, and committing never change the line; moving it is a deliberate edit of this property, never a side effect of
a commit. A development build is `N-internal.<date>.<commit count>` or `N-develop.<date>.<commit count>`; a release publishes plain
versions; each release writes a record (`tools/versioning/baseline/release-FDW-<N>.json`) of the tree it was made from and every package's version.

## Sample Solution Override

The `public/samples/ReferenceSolution/Directory.Build.props` file is intentionally minimal — it only sets `TargetFramework`, `LangVersion`, `NuGetPruneVerbosity`, `Nullable`, and `ImplicitUsings`. The parent `public/Directory.Build.props` still applies via MSBuild's directory walk; samples that need to opt out of behaviour (e.g. central package management) do so explicitly in the parent file.

## Next Steps

- [Package Management](02-03-Package-Management.md) - Central version control
- [Naming Conventions](02-04-Naming-Conventions.md) - Project naming patterns
