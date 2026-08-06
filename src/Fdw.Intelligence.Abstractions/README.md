# Fdw.Intelligence.Abstractions

Intelligence contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (1)

| Type | Kind | Purpose |
|---|---|---|
| `IVectorMemoryStore` | interface | Defines a semantic vector memory store for agent context. Extends the base memory store with semantic… |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `VectorMemoryEntry` | class | Represents a single entry in semantic vector memory. |

## Installation

```bash
dotnet add package Fdw.Intelligence.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Results.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
