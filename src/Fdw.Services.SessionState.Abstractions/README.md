# Fdw.Services.SessionState.Abstractions

The session-state contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (1)

| Type | Kind | Purpose |
|---|---|---|
| `ISessionStateService` | interface | Service for managing per-user session state persistence. Keys follow the format:… |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `SessionStateRecord` | class | Data record for persisted session state entries. |

## Installation

```bash
dotnet add package Fdw.Services.SessionState.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
