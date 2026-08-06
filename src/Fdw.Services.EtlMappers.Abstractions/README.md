# Fdw.Services.EtlMappers.Abstractions

The ETL row-mapper contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (6)

| Type | Kind | Purpose |
|---|---|---|
| `IEtlRowMapper` | interface | Interface for ETL row mappers that convert data reader rows to dictionaries. Implementations can… |
| `IEtlRowMapperFactory` | interface | Non-generic factory interface for creating ETL row mapper instances. |
| `IEtlRowMapperFactory<TMapper, TConfiguration>` | interface | Factory interface for creating ETL row mapper instances. |
| `IEtlRowMapperProvider` | interface | Provider interface for ETL row mappers. Acts as a factory registry for creating mapper instances. |
| `IEtlRowMapperType` | interface | Interface for ETL row mapper type definitions. Mapper types define how to configure, create, and… |
| `IEtlRowMapperType<TMapper, TFactory, TConfiguration>` | interface | Generic interface for mapper types with typed configuration and factory. |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `EtlRowMapperConfiguration` | class | Base configuration class for ETL row mappers. |
| `EtlRowMapperLog` | class | MessageLogging for ETL row mapper operations. EventId range: 8300-8399 |

## Installation

```bash
dotnet add package Fdw.Services.EtlMappers.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results.Abstractions` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
