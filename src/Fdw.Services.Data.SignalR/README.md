# Fdw.Services.Data.SignalR

The SignalR hub that broadcasts data-domain events.

This package declares 1 interface(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `SchemaDiscoveryHubOption` | class | Registers the schema-discovery hub against the collection. |

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `ISchemaDiscoveryHubClient` | interface | Client-side SignalR hub interface for schema discovery progress notifications. |

## Types (7)

| Type | Kind | Purpose |
|---|---|---|
| `SchemaDiscoveryCompletedEvent` | record | Event raised when schema discovery completes successfully. |
| `SchemaDiscoveryFailedEvent` | record | Event raised when schema discovery fails. |
| `SchemaDiscoveryHub` | class | SignalR hub for real-time schema discovery progress notifications. |
| `SchemaDiscoveryNotifier` | class | Default implementation of using SignalR. |
| `SchemaDiscoveryProgressEvent` | record | Event raised to report schema discovery progress. |
| `SchemaDiscoveryStartedEvent` | record | Event raised when schema discovery starts. |
| `SchemaObjectDiscoveredEvent` | record | Event raised when a schema object (table/view) is discovered. |

## Installation

```bash
dotnet add package Fdw.Services.Data.SignalR --prerelease
```

## Dependencies

`Fdw.Services.Data.Abstractions` · `Fdw.SignalR`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
