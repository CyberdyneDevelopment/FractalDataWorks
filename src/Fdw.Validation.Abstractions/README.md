# Fdw.Validation.Abstractions

The validation contract implementations are written against.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (1)

| Type | Kind | Purpose |
|---|---|---|
| `IEntityValidator<in T>` | interface | Defines a validator for an entity type. Allows Abstractions packages (targeting netstandard2.0) to… |

## Installation

```bash
dotnet add package Fdw.Validation.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
