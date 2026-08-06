# Fdw.DevSession.Abstractions

Dev-session contracts — a resumable agent conversation and the state it carries.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (8)

| Type | Kind | Purpose |
|---|---|---|
| `IDevSession` | interface | A single unit of development work — a fix, thread, or conversation — administered from open to done. A… |
| `IDevSessionManager` | interface | The front door for dev sessions: opens, finds, sleeps/wakes, and closes them, and administers nested… |
| `IIsolationLevel` | interface | A strategy for materializing an isolated working copy of a repository for a dev session. |
| `ISessionState` | interface | A lifecycle state of a dev session. |
| `IStrandHandler` | interface | A handler that carries out a routed strand of work within a session. |
| `IStrandState` | interface | A lifecycle state of a concurrent strand within a session. |
| `IWorkspaceCoordinator` | interface | Coordinates concurrent strands of work WITHIN a single dev session (as distinct from managing the… |
| `IWorktreeEngine` | interface | Local-git engine that materializes and manages isolated working copies for dev sessions — the "spin up… |

## Base types (8)

| Type | Kind | Purpose |
|---|---|---|
| `IsolationLevelBase` | class | CRTP base class for strategies. Each concrete strategy supplies its id, name, object-store-sharing… |
| `IsolationLevels` | class | Open collection of dev-session isolation strategies. Uses [MutableTypeCollection] so that consumers can… |
| `SessionStateBase` | class | CRTP base class for options. Each concrete state supplies its id, name, and whether it is terminal… |
| `SessionStates` | class | Open collection of dev-session lifecycle states. Uses [MutableTypeCollection] so consumers can register… |
| `StrandHandlerBase` | class | CRTP base class for options. Each concrete handler supplies its id and name and its own and behavior. |
| `StrandHandlers` | class | Open collection of strand handlers the coordinator routes to. Uses [MutableTypeCollection] and ships… |
| `StrandStateBase` | class | CRTP base class for options. Each concrete state supplies its id, name, and whether it is terminal. |
| `StrandStates` | class | Open collection of strand lifecycle states. Uses [MutableTypeCollection] so consumers can register their… |

## Models and supporting types (20)

| Type | Kind | Purpose |
|---|---|---|
| `AbandonedStrandState` | class | The strand was abandoned: its scope claim was released without merging its work back. Terminal. |
| `BlockedState` | class | The session is blocked awaiting an external actor — typically a human reviewer on a submitted merge… |
| `BranchIsolation` | class | Isolation via a branch only (no separate working tree), sharing the origin's object store. The lightest… |
| `DevSessionTopics` | class | The realtime-bus topic contract for dev-session lifecycle events. Every session/strand transition is… |
| `DoneState` | class | The session is finished — its work merged (or abandoned) and its resources released. Terminal. |
| `HibernatedState` | class | The session is hibernated: a deeper sleep than in which even its warm context is persisted out and the… |
| `IsolatedCopy` | class | A materialized isolated working copy: the branch (and optional working tree) an agent or human works in… |
| `IsolationRequest` | class | Describes an isolated working copy to create for a dev session: which repository, from which base ref,… |
| `MergingState` | class | The session's work is being submitted and merged back — its strands reconciled and the isolated copy… |
| `ReconciledStrandState` | class | The strand's work has been merged back into the session and its scope claim released. Terminal. |
| `ReconcilingStrandState` | class | The strand's work is being merged back into the session (its claimed scope folded in). In flight, not… |
| `ScopeClaim` | class | A granted, non-overlapping claim over a slice of a session's working copy. The coordinator issues one… |

## Installation

```bash
dotnet add package Fdw.DevSession.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
