# Fdw.Services.Terminal.Abstractions

Terminal session contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (3)

| Type | Kind | Purpose |
|---|---|---|
| `ITerminalNotifier` | interface | Service for notifying terminal events to clients. |
| `ITerminalService` | interface | Service for managing persistent terminal sessions. |
| `ITerminalSession` | interface | Represents a persistent terminal session. |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `TerminalDataEventArgs` | class | Event arguments for terminal data received events. |
| `TerminalExitEventArgs` | class | Event arguments for terminal disconnection events. |

## Installation

```bash
dotnet add package Fdw.Services.Terminal.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Results.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
