# Fdw.Sql.Commands.Abstractions

The SQL command contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (7)

| Type | Kind | Purpose |
|---|---|---|
| `ISqlCommand` | interface | — |
| `ISqlCommandCategory` | interface | — |
| `ISqlCommandHandler` | interface | — |
| `ISqlCommandResult` | interface | — |
| `ISqlCommandTranslator` | interface | — |
| `ISqlCommandTranslator<in TCommand, TResult>` | interface | — |
| `ISqlTranslatorRegistry` | interface | — |

## Base types (7)

| Type | Kind | Purpose |
|---|---|---|
| `SqlCommandBase` | class | — |
| `SqlCommandCategories` | class | — |
| `SqlCommandCategoryBase` | class | — |
| `SqlCommandTranslatorBase` | class | — |
| `SqlCommandTranslatorBase<TCommand, TResult>` | class | — |
| `SqlCommandTranslators` | class | — |
| `SqlCommands` | class | — |

## Models and supporting types (15)

| Type | Kind | Purpose |
|---|---|---|
| `MutationResult` | class | Mutation result. The script edits already live in the workspace's in-memory state; just reports which… |
| `MutationResult<T>` | class | Mutation result. The script edits already live in the workspace's in-memory state; just reports which… |
| `AnalysisCategory` | class | — |
| `BuildCategory` | class | — |
| `BuiltInSqlCommandCategories` | class | — |
| `GenerationCategory` | class | — |
| `NavigationCategory` | class | — |
| `ProjectCategory` | class | — |
| `QueryResult<T>` | class | — |
| `RefactoringCategory` | class | — |
| `SearchCategory` | class | — |
| `SqlResultCode` | class | — |

## Installation

```bash
dotnet add package Fdw.Sql.Commands.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Development.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Sql.Workspace` · `Fdw.Types.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
