# Fdw.Types.Abstractions

The `ITypeOption` / `ITypeCollection` contracts that every TypeCollection and every option is written against.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `ICollectionKind` | interface | Interface for CollectionKind type options. |
| `ITypesProvider` | interface | Provider for reading and writing TypeCollection metadata to a database. |

## Base types (2)

| Type | Kind | Purpose |
|---|---|---|
| `CollectionKindBase` | class | Base class for CollectionKind type options. |
| `CollectionKinds` | class | TypeCollection for the kinds of TypeCollections. |

## Models and supporting types (9)

| Type | Kind | Purpose |
|---|---|---|
| `ImmutableKind` | class | Standard immutable TypeCollection (compile-time fixed). |
| `InstanceKind` | class | TypeCollection with pre-created instances instead of types. |
| `MutableKind` | class | Mutable TypeCollection (runtime registration supported). |
| `MutableServiceKind` | class | Mutable Service TypeCollection with runtime registration. |
| `ServiceInstanceKind` | class | Service TypeCollection with pre-created instances. |
| `ServiceKind` | class | Service TypeCollection with factory and configuration support. |
| `TypeCollectionMetadata` | class | Metadata describing a TypeCollection for database persistence. |
| `TypeOptionMetadata` | class | Metadata describing a single TypeOption for database persistence. |
| `TypePropertyMetadata` | class | Metadata describing a property on a TypeOption for database persistence. |

## Installation

```bash
dotnet add package Fdw.Types.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
