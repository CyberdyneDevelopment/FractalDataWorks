# Fdw.Workspace.Roslyn

Roslyn workspace loading and the symbol surface built over it.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `WorkspaceResultCodes` | class | TypeCollection for Workspace result codes. EventId range: 5800-5899 (Workspace domain) |

## Options (30 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ApplyChangesFailedCode` | class | One or more documents could not be written during ApplyChanges. Detail object carries WrittenCount,… |
| `NoActiveSessionCode` | class | No active session exists. |
| `NoSolutionLoadedCode` | class | No solution is loaded. User must load a solution first. |
| `PersistedSessionNotFoundCode` | class | Persisted session file was not found in storage. |
| `ProjectAlreadyLoadedCode` | class | Project is already loaded in the workspace. |
| `ProjectHasDependentsCode` | class | Project cannot be unloaded because other projects depend on it. |
| `ProjectHasPendingChangesCode` | class | Project has pending changes and cannot be unloaded without force. |
| `ProjectIndexUpdateFailedCode` | class | Failed to update project session index. |
| `ProjectLoadFailedCode` | class | Failed to load the project. |
| `ProjectNameRequiredCode` | class | Project name is required but was not provided. |
| `ProjectNotFoundInCurrentSolutionCode` | class | Project was not found in the current (filtered) solution. |
| `ProjectNotFoundInFullSolutionCode` | class | Project was not found in the full solution (before filtering). |
| `ProjectNotFoundInSolutionCode` | class | Project was not found in the solution. |
| `ProjectNotLoadedCode` | class | Project is not loaded in the workspace. |
| `SessionCreationFailedCode` | class | Failed to create a new session. |
| `SessionDeleteFailedCode` | class | Failed to delete the session. |
| `SessionDeserializationFailedCode` | class | Failed to deserialize the session data. |
| `SessionLoadFailedCode` | class | Failed to load the session. |

## Installation

```bash
dotnet add package Fdw.Workspace.Roslyn --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
