# Fdw.Services.DataVault.Abstractions

The data-vault contracts: the command surface for storing and retrieving protected values.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (7)

| Type | Kind | Purpose |
|---|---|---|
| `IDataVault` | interface | Marker interface for a data vault — a sealed, write-and-compare secret store for user-supplied secrets… |
| `IDataVaultConfiguration` | interface | Marker interface for typed data vault body configurations (DefaultDataVaultConfiguration, etc.). Each… |
| `IDataVaultFactory` | interface | Marker interface for data vault factories. |
| `IDataVaultFactory<TVault, TConfiguration>` | interface | Generic interface for data vault factories with typed configuration. |
| `IDataVaultProvider` | interface | Provider for configured data vault instances. |
| `IDataVaultType` | interface | Marker interface for data vault service type definitions. |
| `IDataVaultType<TService, TFactory, TConfiguration>` | interface | Generic interface for data vault service type definitions with typed parameters. |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `DataVaultRequest` | record | Typed lookup request for a data vault — identifies the vault being requested by logical Id and/or Name.… |

## Installation

```bash
dotnet add package Fdw.Services.DataVault.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Connections.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
