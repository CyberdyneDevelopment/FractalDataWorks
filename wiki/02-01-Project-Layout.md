# Project Layout

The Reference Solution demonstrates FractalDataWorks patterns through focused, standalone examples organized into two categories: concepts and integration samples.

## Directory Structure

```
ReferenceSolution/
|-- Directory.Build.props            <-- Shared build properties
|-- Directory.Packages.props         <-- Central package management
|-- nuget.config                     <-- Package source configuration
|
|-- concepts/                        <-- Individual concept demonstrations
|   |-- 01-type-collections/         <-- TypeCollection patterns
|   |-- 02-service-types/            <-- ServiceType with DI
|   |-- 03-message-logging/          <-- MessageLogging source generator
|   |-- 04-results/                  <-- IGenericResult patterns
|   |-- 05-configuration/            <-- Configuration types
|   |-- 06-data-layer/               <-- DataGateway and commands
|   |-- 07-translators/              <-- Data translators
|
|-- integration/                     <-- Multi-concept integration samples
    |-- 01-pipeline/                 <-- Pipeline execution
    |-- 02-custom-connection/        <-- Custom connection types
    |-- 03-full-translator/          <-- Complete translator example
    |-- 04-three-phase-di/           <-- Three-phase DI registration
```

## Concept Projects

Each concept folder contains a standalone project demonstrating a single FractalDataWorks pattern:

| Folder | Concept | Description |
|--------|---------|-------------|
| `01-type-collections` | TypeCollection | Extensible type-safe alternatives to enums |
| `02-service-types` | ServiceType | TypeCollections with DI registration |
| `03-message-logging` | MessageLogging | Source-generated structured logging |
| `04-results` | IGenericResult | Railway-oriented programming |
| `05-configuration` | Configuration | Typed configuration binding |
| `06-data-layer` | DataGateway | Data access patterns |
| `07-translators` | Translators | Data format translation |

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

From [`concepts/01-type-collections/src/Reference.TypeCollections/Reference.TypeCollections.csproj`](../../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Reference.TypeCollections.csproj):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FractalDataWorks.Collections" />
    <PackageReference Include="FractalDataWorks.Collections.SourceGenerators" />
  </ItemGroup>
</Project>
```

Note: Package versions are managed centrally in `Directory.Packages.props`, so projects reference packages without explicit versions.

## Integration Samples

Integration samples demonstrate multiple concepts working together:

| Folder | Sample | Concepts Combined |
|--------|--------|-------------------|
| `01-pipeline` | Pipeline execution | Results, MessageLogging, Configuration |
| `02-custom-connection` | Custom connection | ServiceTypes, Configuration, DI |
| `03-full-translator` | Complete translator | Translators, DataGateway, Results |
| `04-three-phase-di` | Three-phase DI | ServiceTypes, Factory pattern, DI |

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

## Next Steps

- [Directory.Build.props](02-02-Directory-Build-Props.md) - Shared build configuration
- [Package Management](02-03-Package-Management.md) - Central version management
- [TypeCollections Guide](04-01-Overview.md) - TypeCollection patterns in depth
