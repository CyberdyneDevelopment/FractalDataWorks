# Fdw.Data.DataContainers.Abstractions

Container contracts — the addressable unit a command names, standing in for a table, file or endpoint.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (10)

| Type | Kind | Purpose |
|---|---|---|
| `IContainerWriteMode` | interface | Interface for container write modes. Extends ITypeOption to enable TypeCollection discovery. |
| `IDataContainer` | interface | Represents a data container that defines the physical format and structure of data. DataContainers… |
| `IDataContainerResultCode` | interface | Interface for DataContainer result codes. |
| `IDataReader` | interface | Interface for reading data from containers in a streaming fashion. Provides both synchronous and… |
| `IDataRow` | interface | Represents a single row of data with field access by name or ordinal. Provides high-performance access… |
| `IDataWriter` | interface | Interface for writing data to containers in a streaming fashion. Provides both synchronous and… |
| `IReaderStatistics` | interface | Provides statistics about a data reading operation. |
| `IRuntimeDataSet` | interface | Represents an in-memory dataset with LINQ-like query operations. |
| `IWriteTransaction` | interface | Represents a transaction for atomic write operations. |
| `IWriterStatistics` | interface | Provides statistics about a data writing operation. |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `ContainerMessage` | class | Base class for all data container-related messages. |
| `ContainerWriteModeBase` | class | Base class for container write modes. |
| `DataContainerResultCodeBase` | class | Base class for DataContainer result codes. |
| `DataContainerResultCodes` | class | TypeCollection for DataContainer result codes. EventId range: 4100-4199 (Data.DataContainers domain) |

## Models and supporting types (20)

| Type | Kind | Purpose |
|---|---|---|
| `AppendWriteMode` | class | Append new data to existing data. |
| `ContainerMetadata` | class | Metadata about a specific data container instance. |
| `ContainerMetrics` | class | Represents metrics about a data container's characteristics. |
| `ContainerWriteModes` | class | TypeCollection for container write modes. |
| `CreateNewWriteMode` | class | Get new container, fail if it already exists. |
| `DataRow` | class | Efficient implementation of IDataRow using array storage. |
| `DataSchema` | class | Concrete implementation of IDataSchema. |
| `DataSchemaExtensions` | class | Extension methods for IDataSchema. |
| `FieldConversionFailedCode` | class | Cannot convert field value to target type. |
| `FieldRequiredCode` | class | Required field value is null. |
| `FieldTypeMismatchCode` | class | Field value has incorrect type. |
| `OverwriteWriteMode` | class | Overwrite any existing data completely. |

## Installation

```bash
dotnet add package Fdw.Data.DataContainers.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
