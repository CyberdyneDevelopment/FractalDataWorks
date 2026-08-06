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
<Configurations>Debug;Debug-Local;Develop;Develop-Local;Release;Release-Local;Test;Test-Local</Configurations>
<IsLocalConfiguration>false</IsLocalConfiguration>
<IsLocalConfiguration Condition="$(Configuration.EndsWith('-Local'))">true</IsLocalConfiguration>
<BaseConfiguration>$(Configuration)</BaseConfiguration>
<BaseConfiguration Condition="'$(IsLocalConfiguration)' == 'true'">$(Configuration.Replace('-Local', ''))</BaseConfiguration>
```

`-Local` configurations resolve packages from `$(LocalNugetFolder)` (see [Local Package Development](01-04-Local-Package-Development.md)). `BaseConfiguration` strips the suffix so consumers can branch on the underlying build mode.

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

The version is a single hand-set number — MinVer and tag-driven stamping are removed. The repo's
`Directory.Build.props` declares it once and every package in the repo is stamped with it:

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<VersionSuffix>rc.1</VersionSuffix>
```

Branching, building, and committing never change the number; bumping it is a deliberate edit of
these two properties (kept in lockstep across all ecosystem repos), never a side effect of a commit.

### Local NuGet config selection

```xml
<PropertyGroup>
  <LocalNugetConfigFileName Condition="'$(LocalNugetConfigFileName)' == ''">Fdw.Local.nuget.config</LocalNugetConfigFileName>
</PropertyGroup>

<PropertyGroup Condition="'$(IsLocalConfiguration)' == 'true' AND '$(LocalNugetFolder)' != ''">
  <RestoreConfigFile>$(LocalNugetFolder)\$(LocalNugetConfigFileName)</RestoreConfigFile>
</PropertyGroup>
```

The local config is selected only when **both** the configuration ends in `-Local` and `LocalNugetFolder` is set.

## Sample Solution Override

The `public/samples/ReferenceSolution/Directory.Build.props` file is intentionally minimal — it only sets `TargetFramework`, `LangVersion`, `NuGetPruneVerbosity`, `Nullable`, and `ImplicitUsings`. The parent `public/Directory.Build.props` still applies via MSBuild's directory walk; samples that need to opt out of behaviour (e.g. central package management) do so explicitly in the parent file.

## Next Steps

- [Package Management](02-03-Package-Management.md) - Central version control
- [Naming Conventions](02-04-Naming-Conventions.md) - Project naming patterns
- [Local Package Development](01-04-Local-Package-Development.md) - Working with local NuGet packages
