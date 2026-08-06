# Project Layout

The Reference Solution demonstrates FractalDataWorks patterns through focused, standalone concept tutorials.

## Directory Structure

```
ReferenceSolution/
|-- Directory.Build.props            <-- Shared build properties
|-- Directory.Packages.props         <-- Central package management
|-- nuget.config                     <-- Package source configuration
|
|-- concepts/                        <-- Individual concept demonstrations
    |-- 01-type-collections/         <-- TypeCollection patterns
    |-- 02-service-types/            <-- ServiceType with DI
    |-- 03-message-logging/          <-- MessageLogging source generator
    |-- 05-configuration/            <-- Configuration types
    |-- 06-data-layer/               <-- DataGateway and commands
```

## Concept Projects

Each concept folder contains a standalone project demonstrating a single FractalDataWorks pattern:

| Folder | Concept | Description |
|--------|---------|-------------|
| `01-type-collections` | TypeCollection | Extensible type-safe alternatives to enums |
| `02-service-types` | ServiceType | TypeCollections with DI registration |
| `03-message-logging` | MessageLogging | Source-generated structured logging |
| `05-configuration` | Configuration | Typed configuration binding |
| `06-data-layer` | DataGateway | Data access patterns |

## Concept Project Structure

Each concept follows a consistent layout:

```
concepts/01-type-collections/
|-- README.md                        <-- Concept documentation
|-- src/
    |-- Reference.TypeCollections/
        |-- Reference.TypeCollections.csproj
        |-- Program.cs
        |-- Basic/                   <-- TypeCollection implementation
        |-- Mutable/                 <-- MutableTypeCollection
        |-- Instance/                <-- TypeInstanceCollection
```

## Project File Example

From [`concepts/01-type-collections/src/Reference.TypeCollections/Reference.TypeCollections.csproj`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Reference.TypeCollections.csproj):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Fdw.Collections" />
    <PackageReference Include="Fdw.Collections.SourceGenerators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

Note: Package versions are managed centrally in `Directory.Packages.props`, so projects reference packages without explicit versions.

## Running Examples

Each concept can be run independently:

```bash
# Run TypeCollections example
cd public/samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections
dotnet run

# Run ServiceTypes example
cd public/samples/ReferenceSolution/concepts/02-service-types/src/Reference.ServiceTypes
dotnet run

# Run MessageLogging example
cd public/samples/ReferenceSolution/concepts/03-message-logging/src/Reference.MessageLogging
dotnet run
```

## Framework Project Tiers

The framework source projects (`public/src/`) follow a tiered architecture:

| Tier | Suffix | Purpose | Example |
|------|--------|---------|---------|
| Abstractions | `.Abstractions` | Interfaces, base classes (`netstandard2.0`) | `Services.Connections.Abstractions` |
| Implementation | (none) | Providers, collections, logging | `Services.Connections` |
| Concrete | `.{Implementation}` | Specific implementations | `Services.Connections.MsSql` |
| **Endpoints** | `.Endpoints` | Generic base endpoints (FastEndpoints) | `Services.Connections.Endpoints` |

### Per-Domain Endpoint Packages

Each service domain provides its own `.Endpoints` NuGet package containing generic base endpoint classes and shared DTOs. Consumers create thin closures that close the generic type parameters with concrete types.

| Package | Domain |
|---------|--------|
| `Fdw.Services.Connections.Endpoints` | Connection CRUD + test |
| `Fdw.Services.Data.Endpoints` | DataStores, DataSets, Containers |
| `Fdw.Services.Pipelines.Endpoints` | Pipeline status and management |
| `Fdw.Services.Scheduling.Endpoints` | Schedule management |
| `Fdw.Services.Users.Endpoints` | User CRUD |
| `Fdw.Services.Authorization.Endpoints` | Roles, permissions, user-roles |
| `Fdw.Services.Multitenancy.Endpoints` | Tenant management |
| `Fdw.Services.Quality.Endpoints` | Data quality rules |
| `Fdw.Services.Catalog.Endpoints` | Glossary, annotations, search |
| `Fdw.Operations.Endpoints` | Executions, dataflow, configuration metadata |
| `Fdw.Schema.Endpoints` | Schema discovery and import |
| `Fdw.Web.Search.Endpoints` | Full-text search |
| `Fdw.UI.Themes.Endpoints` | Theme CRUD and defaults |
| `Fdw.Services.Authentication.Endpoints` | Authentication (login, refresh, logout, user info) |
| `Fdw.Calculations.Endpoints` | Calculation execution and type listing |

See [API Endpoints](12-07-API-Endpoints.md) for architecture details and [Customizing Endpoints](12-08-Customizing-Endpoints.md) for the thin closure pattern.

## Next Steps

- [Directory.Build.props](02-02-Directory-Build-Props.md) - Shared build configuration
- [Package Management](02-03-Package-Management.md) - Central version management
- [TypeCollections Guide](04-01-Overview.md) - TypeCollection patterns in depth
