# Fdw.Workspace.Management

Workspace and solution management.

This package declares 2 interface(s), 1 service/provider type(s), 1 model(s).

## Contracts (2)

| Type | Kind | Purpose |
|---|---|---|
| `IWorkspaceManager` | interface | Manages multiple Roslyn workspaces with session persistence and lifecycle management. |
| `IWorkspaceSessionStore` | interface | Abstracts the persistence mechanism for workspace sessions. |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `WorkspaceManager` | class | Default implementation of that manages multiple Roslyn workspaces with session persistence support. |

## Records (1)

| Type | Kind | Purpose |
|---|---|---|
| `SnapshotRecord` | class | Represents a snapshot record within a session. |

## Types (6)

| Type | Kind | Purpose |
|---|---|---|
| `FileBasedSessionStore` | class | File-based implementation of that persists sessions as JSON files in a specified directory. |
| `InMemorySessionStore` | class | In-memory implementation of for testing and scenarios where persistence is not required. |
| `SessionInfo` | class | Provides information about a saved workspace session. |
| `WorkspaceInfo` | class | Provides information about a loaded workspace. |
| `WorkspaceManagementLog` | class | MessageLogging methods for workspace management operations. EventId range: 9016-9050. |
| `WorkspaceSession` | class | Represents a serializable workspace session for persistence. |

## Installation

```bash
dotnet add package Fdw.Workspace.Management --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Workspace.Roslyn`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
