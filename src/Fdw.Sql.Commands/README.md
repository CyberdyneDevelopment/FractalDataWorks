# Fdw.Sql.Commands

SQL command types and the catalogue that exposes them.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SqlAnalysisTranslators` | class | — |
| `SqlBuildTranslators` | class | — |
| `SqlGenerationTranslators` | class | — |
| `SqlNavigationTranslators` | class | — |
| `SqlProjectTranslators` | class | — |
| `SqlRefactoringTranslators` | class | — |
| `SqlSearchTranslators` | class | — |
| `SqlWorkspaceTranslators` | class | — |

## Options (106 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CreateSnapshotTranslator` | class | Generates a placeholder snapshot ID. The handler intercepts the result and patches the real SnapshotId… |
| `AddScriptCommand` | class | — |
| `AddScriptTranslator` | class | — |
| `AnalyzeComplexityCommand` | class | — |
| `AnalyzeComplexityTranslator` | class | — |
| `AnalyzeCouplingCommand` | class | — |
| `AnalyzeCouplingTranslator` | class | — |
| `AnalyzeDependenciesCommand` | class | — |
| `AnalyzeDependenciesTranslator` | class | — |
| `AnalyzeIndexCoverageCommand` | class | — |
| `AnalyzeIndexCoverageTranslator` | class | — |
| `ApplyWorkspaceChangesCommand` | class | — |
| `ApplyWorkspaceChangesTranslator` | class | — |
| `BuildProjectCommand` | class | — |
| `BuildProjectTranslator` | class | — |
| `CompareToBaselineCommand` | class | — |
| `CompareToBaselineTranslator` | class | — |
| `CreateSnapshotCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.Sql.Commands --prerelease
```

## Dependencies

`Fdw.Sql.Commands.Abstractions` · `Fdw.Sql.Workspace`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
