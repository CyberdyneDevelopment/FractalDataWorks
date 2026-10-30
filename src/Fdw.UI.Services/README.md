# Fdw.UI.Services

Supporting services for the UI layer.

This package declares 4 interface(s), 1 service/provider type(s).

## Contracts (4)

| Type | Kind | Purpose |
|---|---|---|
| `ICommand` | interface | Represents a command that can be executed and undone. |
| `IPipelineValidator` | interface | Validates pipeline definitions. |
| `IUndoRedoManager` | interface | Manages undo/redo operations for commands. |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `UndoRedoManager` | class | Manages undo/redo operations for commands. |

## Types (6)

| Type | Kind | Purpose |
|---|---|---|
| `CompositeCommand` | class | Command that executes multiple commands as a single unit. |
| `PipelineValidator` | class | Validates pipeline definitions. |
| `UIServiceCollectionExtensions` | class | Extension methods for registering UI services. |

## Installation

```bash
dotnet add package Fdw.UI.Services --prerelease
```

## Dependencies

`Fdw.Messages` · `Fdw.Results` · `Fdw.UI.Pipelines.Clients.Models`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; edit by hand, nothing regenerates it -->
