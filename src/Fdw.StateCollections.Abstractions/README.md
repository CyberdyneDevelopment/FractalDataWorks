# Fdw.StateCollections.Abstractions

The state-machine contracts: states, transitions and the guards a machine enforces.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (6)

| Type | Kind | Purpose |
|---|---|---|
| `IStateContext<TState>` | interface | Per-entity execution context handed to and . Composes — never redeclares — the cross-cutting fields an… |
| `IStateMachine<TState>` | interface | Per-entity facade an engine exposes to callers. Wraps the smart-state graph + persistence hook + handler… |
| `IStateMachineFactory<TState, TEntity>` | interface | Builds a per-entity . Implementations hold the shared configuration (state graph, handler chain) and… |
| `IStateOption<TSelf>` | interface | A "smart state" — a that owns its own outbound transition table via and its own entry/exit behavior.… |
| `IStateTransition<TState>` | interface | Event record describing a successful transition. Emitted by the engine after persistence and dispatched… |
| `IStateTransitionHandler<TState>` | interface | Receives a successful transition after the engine has persisted the new state. Domains register one or… |

## Installation

```bash
dotnet add package Fdw.StateCollections.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
