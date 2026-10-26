# Fdw.Commands.Data.Fluency

The fluent builder surface over data commands — `Query.From<T>(…).Where(…).Build()` and its `Where*` operators.

This package declares 10 service/provider type(s).

## Services (10)

| Type | Kind | Purpose |
|---|---|---|
| `DataStoreBuilder` | class | Intermediate builder for selecting a path within a data store. |
| `DataStorePathBuilder` | class | Intermediate builder for selecting a container within a DataStore path. |
| `DeleteCommandBuilder` | class | Builder for delete commands with fluent filter construction. The terminal method returns a that bundles… |
| `FilterConditionBuilder<T, TProperty>` | class | Fluent builder for filter conditions with type-safe operator methods. Created by… |
| `FindCommandBuilder<T>` | class | Fluent builder for to eliminate boilerplate and provide a clean API for constructing cross-field search… |
| `InsertBatchBuilder<T>` | class | Builder for batch insert commands. The terminal method returns a that bundles the address-free command… |
| `InsertSingleBuilder<T>` | class | Builder for single-entity insert commands. The terminal method returns a that bundles the address-free… |
| `NearestCommandBuilder<T>` | class | Builds a nearest-neighbour search command and its target. Every part is required, and Build names the one that is missing. |
| `QueryCommandBuilder<T>` | class | Fluent builder for QueryCommand to eliminate boilerplate and provide clean API. Allows building complex… |
| `UpdateCommandBuilder<T>` | class | Builder for update commands with fluent filter construction. The terminal method returns a that bundles… |

## Types (9)

| Type | Kind | Purpose |
|---|---|---|
| `DataQuery` | class | Direct factory method for creating query builders with full path specification. |
| `DataStores` | class | Static entry point for hierarchical data store access. Provides discoverable, semantic query… |
| `Delete` | class | Fluent builder for delete commands. |
| `FilterOperatorExtensions` | class | Extension methods providing shortcuts for common filter and aggregation operators. Reduces boilerplate… |
| `Find` | class | Fluent entry point for building find (cross-field search) commands. |
| `Insert` | class | Fluent builder for insert commands. |
| `Query` | class | Alias for DataQuery for shorter syntax. |
| `Update` | class | Fluent builder for update commands. |
| `Vectorized` | class | Entry point for the vectorized builders: a search only a store that holds vectors can answer. |

## Installation

```bash
dotnet add package Fdw.Commands.Data.Fluency --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- tables checked against source at adf451066 on 2026-10-25 -->
