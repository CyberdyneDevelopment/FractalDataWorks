# Fdw.Commands.Data

The data command model — `QueryCommand`, `InsertCommand`, `UpdateCommand`, `DeleteCommand` and the filter expressions they carry.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `DdlCommandTypes` | class | TypeCollection for DDL command types (schema operations). |
| `FederationStrategies` | class | Collection of federation strategies for data operations. |
| `ForeignKeyActions` | class | TypeCollection for foreign key referential action types. |
| `JoinExecutors` | class | Collection of join executor strategies. |
| `AlterTableOperationTypes` | class | — |

## Options (37 declared)

| Type | Kind | Purpose |
|---|---|---|
| `BulkInsertCommand<T>` | class | Bulk insert command for high-performance batch inserts using database-specific bulk mechanisms. Returns… |
| `ConfigurationDeleteCommand` | class | Configuration delete command for soft-delete via version-on-write pattern. Marks the current row as… |
| `ConfigurationSaveCommand<T>` | class | Configuration save command for version-on-write upsert operations. Marks the current row as non-current… |
| `CrossJoinExecutor` | class | Executes a cross join (Cartesian product) between two record sets. Returns all possible combinations of… |
| `DeleteCommand` | class | Delete command for removing records (DELETE operation). Returns the number of affected rows. |
| `FederationStrategies` | class | Collection of federation strategies for data operations. |
| `FindCommand<T>` | class | Find command for cross-field text search across a container's string fields. Returns matched records… |
| `FullJoinExecutor` | class | Executes a full outer join between two record sets. Returns all rows from both sources, with defaults… |
| `InnerJoinExecutor` | class | Executes an inner join between two record sets. Only returns rows where a match exists in both sources. |
| `InsertCommand<T>` | class | Insert command for adding new records (INSERT operation). Returns the number of affected rows or… |
| `JoinExecutors` | class | Collection of join executor strategies. |
| `LeftJoinExecutor` | class | Executes a left outer join between two record sets. Returns all rows from the left source, with matching… |
| `OptimizedStrategy` | class | Optimized federation strategy - analyzes and chooses best execution approach. |
| `ParallelStrategy` | class | Parallel federation strategy - executes all source queries concurrently. |
| `QueryCommand<T>` | class | Query command for retrieving data (SELECT operation). Returns an enumerable collection of typed results… |
| `RightJoinExecutor` | class | Executes a right outer join between two record sets. Returns all rows from the right source, with… |
| `SequentialStrategy` | class | Sequential federation strategy - executes source queries one at a time. |
| `TruncateCommand` | class | Truncate command for emptying a container (removes ALL records). Returns the number of affected rows. |

## Installation

```bash
dotnet add package Fdw.Commands.Data --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
