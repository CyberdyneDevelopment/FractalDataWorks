# Fdw.Services.Connections.RoslynWorkspace.Abstractions

Contracts for the Roslyn workspace connection.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (3)

| Type | Kind | Purpose |
|---|---|---|
| `IRoslynWorkspaceClient` | interface | Typed primitive client for RoslynWorkspace operations. This is the cross-boundary surface connectors… |
| `IRoslynWorkspaceConnection` | interface | Marker interface for a RoslynWorkspace connection. Exposes the solution path, mode, and the typed… |
| `IRoslynWorkspaceMode` | interface | Marker interface for RoslynWorkspace operating modes. |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `RoslynWorkspaceModeBase` | class | Base class for RoslynWorkspace operating modes. |
| `RoslynWorkspaceModes` | class | TypeCollection for RoslynWorkspace operating modes. |
| `RoslynWorkspaceResultCodeBase` | class | Base class for RoslynWorkspace connection result codes. |
| `RoslynWorkspaceResultCodes` | class | TypeCollection for RoslynWorkspace connection result codes. Result codes use categorized numbers (prefix… |

## Models and supporting types (15)

| Type | Kind | Purpose |
|---|---|---|
| `GetSymbolSourceCapability` | class | Get symbol source capability — retrieves source text for a Roslyn symbol by its DocumentationCommentId.… |
| `InvalidSymbolIdCode` | class | The provided symbol id is not a valid Roslyn DocumentationCommentId. |
| `LiveMode` | class | Live mode — the workspace is kept resident in memory for repeated queries. Suitable for interactive use… |
| `ModeRequiresLiveCode` | class | The requested operation requires Live mode but the connection is in Snapshot mode. |
| `RoslynSymbolMatch` | record | Identifier-shaped description of a Roslyn symbol — the wire form for cross-boundary symbol references.… |
| `RoslynWorkspaceConnectionLog` | class | MessageLogging for the RoslynWorkspace connection. EventId range: 9600-9628 (9605-9608 trace, 9613-9616… |
| `SnapshotMode` | class | Snapshot mode — the workspace loads on first command and disposes immediately after. Suitable for… |
| `SolutionFileNotFoundCode` | class | The .sln file specified in SolutionPath does not exist on disk. |
| `SolutionPathNotConfiguredCode` | class | The RoslynWorkspace connection configuration is missing the required SolutionPath. |
| `SymbolNotFoundCode` | class | The requested symbol was not found in the workspace. |
| `WorkspaceEdge` | record | A directed dependency edge between two projects in the workspace graph. |
| `WorkspaceGraph` | record | Represents the project-dependency graph of a Roslyn workspace. Returned by . |

## Installation

```bash
dotnet add package Fdw.Services.Connections.RoslynWorkspace.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services.Connections.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
