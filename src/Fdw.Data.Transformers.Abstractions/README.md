# Fdw.Data.Transformers.Abstractions

Field-level transformer contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IDataTransformer` | interface | Base interface for all data transformers. Transforms data from one schema/format to another. |
| `IDataTransformer<TIn, TOut>` | interface | Generic interface for typed data transformers. |

## Base types (2)

| Type | Kind | Purpose |
|---|---|---|
| `DataTransformers` | class | TypeCollection for all data transformers. Source generator discovers all types marked with… |
| `TransformerBase<TIn, TOut>` | class | Base class for implementing data transformers with type safety. |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `TransformContext` | class | Provides context and metadata for transformation operations. |

## Installation

```bash
dotnet add package Fdw.Data.Transformers.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
