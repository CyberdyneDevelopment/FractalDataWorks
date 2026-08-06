# Fdw.Data.Importers.Abstractions

Contracts for importing external data into FDW containers.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (4)

| Type | Kind | Purpose |
|---|---|---|
| `ISchemaImportPersister` | interface | Persists discovered schema to the configuration database. Maps the discovered hierarchy… |
| `ISchemaImporter` | interface | Base interface for schema importers that discover schema from external sources. Implementations… |
| `ISchemaImporter<TConfiguration>` | interface | Generic schema importer interface with strongly-typed importer configuration. |
| `ISchemaImporterResultCode` | interface | Interface for Schema Importer result codes. |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `SchemaImporterBase<TConfig>` | class | Base class for schema importer implementations. |
| `SchemaImporterResultCodeBase` | class | Base class for Schema Importer result codes. |
| `SchemaImporterResultCodes` | class | TypeCollection for Schema Importer result codes. Codes use the categorized-number catalog scheme (Id ==… |
| `SchemaImporters` | class | TypeCollection for schema importers. Implementations use [TypeOption(typeof(SchemaImporters), "name",… |

## Models and supporting types (8)

| Type | Kind | Purpose |
|---|---|---|
| `ImportFailedCode` | class | Import operation failed. |
| `ImportedContainer` | class | Imported Container (table, endpoint, file, etc.). |
| `ImportedDataStore` | class | Imported DataStore configuration. |
| `ImportedField` | class | Imported Field (column, property, etc.). |
| `SchemaImportResult` | class | Result of a schema import operation. |
| `SchemaImportSyncResult` | class | Result of a schema sync operation. |
| `SchemaImporterOptions` | class | Options for schema import operations. |
| `SourceRequiredCode` | class | Source was null or empty. |

## Installation

```bash
dotnet add package Fdw.Data.Importers.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.Results` · `Fdw.Services.Connections`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
