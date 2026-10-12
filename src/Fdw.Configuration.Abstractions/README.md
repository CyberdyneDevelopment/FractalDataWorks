# Fdw.Configuration.Abstractions

The configuration contracts — the provider surface, the command shapes, and the parent-header / typed-body model that keeps polymorphic configuration strongly typed.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (4)

| Type | Kind | Purpose |
|---|---|---|
| `IConfigurationDdlProvider` | interface | Interface for configuration classes that provide DDL definitions. Implemented by generated code from… |
| `IConfigurationSourceProvider` | interface | Provides configuration sources for database-backed configuration. |
| `IEnvironmentType` | interface | Marker interface for deployment environment type options. |
| `IForeignKeyAction` | interface | Interface for foreign key referential actions. |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationBase<T>` | class | Base class for all configuration types in the Fdw framework. Provides common metadata properties for… |
| `EnvironmentTypeBase` | class | Abstract base class for deployment environment types (Local, Dev, QA, Prod). |
| `EnvironmentTypes` | class | TypeCollection of deployment environment types: Local, Dev, QA, Prod. Used as a [ValuesFrom] source on… |
| `ForeignKeyActionBase` | class | Base class for foreign key referential actions. |
| `ForeignKeyActions` | class | TypeCollection for foreign key referential actions. |

## Models and supporting types (16)

| Type | Kind | Purpose |
|---|---|---|
| `ColumnDefinition` | class | Represents a column definition for DDL generation. |
| `ConfigurationPropertyAttribute` | class | Provides additional metadata for configuration properties. |
| `ConfigurationSectionAttribute` | class | Defines a section in the configuration UI form. |
| `DdlDefinition` | class | Represents a complete DDL definition for a configuration table. |
| `DevEnvironmentType` | class | Shared development environment — team integration, shared services. |
| `ForeignKeyDefinition` | class | Represents a foreign key definition for DDL generation. |
| `IndexDefinition` | class | Represents an index definition for DDL generation. |
| `LocalEnvironmentType` | class | Local development environment — developer machine, local services. |
| `ProdEnvironmentType` | class | Production environment — live system, real data, full security. |
| `QaEnvironmentType` | class | QA / test environment — pre-production validation and testing. |
| `TypeCollectionReferenceInfo` | class | Information about a TypeCollection reference on a configuration property. Used to track which properties… |
| `ValuesFromAttribute` | class | Specifies that the values for this property should be sourced from a TypeCollection. |

## Installation

```bash
dotnet add package Fdw.Configuration.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Results` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
