# Fdw.Data.DataNodes

The shared node model the DataStore tree is built from.

This package declares 1 interface(s), 2 service/provider type(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IDataStoreBuilderSelector` | interface | Selects the per-transport for a given store configuration. |

## Services (2)

| Type | Kind | Purpose |
|---|---|---|
| `ConfiguredDataStoreProvider` | class | Resolves DataStores as the canonical navigable tree, from a configuration source and a per-transport… |
| `GenericDataStoreBuilder` | class | The generic per-transport for non-SQL transports (HTTP, file). Builds generic nodes whose response and… |

## Types (7)

| Type | Kind | Purpose |
|---|---|---|
| `ConfiguredDataStoreProviderLog` | class | Source-generated logging methods for . |
| `ContainerComposition` | class | Resolves the per-container composition values (response format + response-shaping metadata) that a… |
| `ContainerKey` | class | Runtime implementation of . Groups one or more entries under a named key type. Constructed by the… |
| `ContainerKeyField` | class | Runtime implementation of . Constructed from data.DataContainerKeyField rows by the per-transport… |
| `DataField` | class | Generic runtime implementation of — a leaf . |
| `DataStoreBuilderBase` | class | Shared base for the per-transport s. Owns the transport-agnostic assembly of the uniform tree (store →… |
| `DataStoreLoaderLog` | class | MessageLogging for DataStoreLoader operations. EventId range: 5185-5199 (loader) plus 5237 (runtime node… |

## Installation

```bash
dotnet add package Fdw.Data.DataNodes --prerelease
```

## Dependencies

`Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Data.Configuration` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
