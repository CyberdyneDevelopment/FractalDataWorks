# Fdw.Services.Connections.RoslynWorkspace

A connection whose backend is a Roslyn workspace, so a solution can be queried through the same gateway as a database.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SymbolKindScores` | class | TypeCollection of scoring weights per Roslyn SymbolKind, keyed by the enum's name. Look up with… |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `RoslynWorkspaceConnectionType` | class | Connection type definition for RoslynWorkspace connections. Registers and binds from… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `RoslynWorkspaceConnectionConfiguration` | class | Configuration for RoslynWorkspace connections. Standalone typed body POCO — no longer inherits from .… |

## Installation

```bash
dotnet add package Fdw.Services.Connections.RoslynWorkspace --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.RoslynWorkspace.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Workspace.Roslyn`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
