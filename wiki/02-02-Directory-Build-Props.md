# Directory.Build.props

The `Directory.Build.props` file provides shared MSBuild properties for all projects in the solution.

## Full Configuration

From [`Directory.Build.props`](../../samples/ReferenceSolution/Directory.Build.props):

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <PropertyGroup>
    <RootNamespace>$(MSBuildProjectName)</RootNamespace>
    <AssemblyName>$(MSBuildProjectName)</AssemblyName>
  </PropertyGroup>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <!-- Local NuGet Development Configuration -->
  <PropertyGroup>
    <!-- Default config filename - can be overridden in project files -->
    <LocalNugetConfigFileName Condition="'$(LocalNugetConfigFileName)' == ''">Fdw.Local.nuget.config</LocalNugetConfigFileName>
  </PropertyGroup>

  <!-- Use local NuGet config when LocalNugetFolder is set and not in Release -->
  <PropertyGroup Condition="'$(Configuration)' != 'Release' AND '$(LocalNugetFolder)' != ''">
    <RestoreConfigFile>$(LocalNugetFolder)\$(LocalNugetConfigFileName)</RestoreConfigFile>
  </PropertyGroup>
</Project>
```

## Key Settings

### Target Framework

```xml
<TargetFramework>net10.0</TargetFramework>
```

All projects target .NET 10.

### Nullable Reference Types

```xml
<Nullable>enable</Nullable>
```

Enables nullable reference type analysis across all projects.

### Warnings as Errors

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Treats all warnings as errors to ensure code quality.

### Central Package Management

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

Enables central package version management. See [Package Management](02-03-Package-Management.md).

## Local NuGet Development

From [`Directory.Build.props:21-30`](../../samples/ReferenceSolution/Directory.Build.props#L21-L30):

```xml
<!-- Local NuGet Development Configuration -->
<PropertyGroup>
  <!-- Default config filename - can be overridden in project files -->
  <LocalNugetConfigFileName Condition="'$(LocalNugetConfigFileName)' == ''">Fdw.Local.nuget.config</LocalNugetConfigFileName>
</PropertyGroup>

<!-- Use local NuGet config when LocalNuGetFolder is set and not in Release -->
<PropertyGroup Condition="'$(Configuration)' != 'Release' AND '$(LocalNugetFolder)' != ''">
  <RestoreConfigFile>$(LocalNugetFolder)\$(LocalNugetConfigFileName)</RestoreConfigFile>
</PropertyGroup>
```

This configuration enables local NuGet package development by pointing to a local feed when `LocalNugetFolder` is set and the build configuration is not Release.

## Next Steps

- [Package Management](02-03-Package-Management.md) - Central version control
- [Naming Conventions](02-04-Naming-Conventions.md) - Project naming patterns
- [Local Package Development](01-04-Local-Package-Development.md) - Working with local NuGet packages
