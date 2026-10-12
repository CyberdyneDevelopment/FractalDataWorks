# Fdw.Data.Lineage

Data lineage — the node and edge model that records where a field's value came from and what depends on it.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `LineageEdgeTypes` | class | Collection of lineage edge types. |
| `LineageNodeStatuses` | class | Collection of lineage node status types. |
| `LineageNodeTypes` | class | Collection of lineage node types. |

## Options (23 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CalculationNodeType` | class | A calculation node (transforms or aggregates data). |
| `ConnectionNodeType` | class | A connection to an external data store. |
| `ConsumesEdgeType` | class | Pipeline consumes a DataSet. |
| `ContainsEdgeType` | class | A parent orchestration node contains a child node. Used for: Project → Contains → Stage, Stage →… |
| `DataSetNodeType` | class | A logical dataset node (can be produced/consumed by pipelines). |
| `DependsOnEdgeType` | class | A pipeline within a step depends on another pipeline completing before it can start. Used for: Pipeline… |
| `DerivesFromEdgeType` | class | A DataSet derives its data from another DataSet (source DataSet → derived DataSet). Used for: DataSet →… |
| `ErrorStatus` | class | Node has errors. |
| `ExternalSystemNodeType` | class | An external system (API, file system, etc.). |
| `HealthyStatus` | class | Node is healthy and operational. |
| `InputsFromEdgeType` | class | Calculation inputs from a DataSet (Calculation←DataSet). |
| `PipelineNodeType` | class | An ETL/ELT pipeline node. |
| `ProducesDataSetEdgeType` | class | Calculation produces a DataSet (Calculation→DataSet). |
| `ProducesEdgeType` | class | Pipeline produces a DataSet. |
| `ProjectNodeType` | class | An ETL project orchestration node — the root of the Project → Stage → Step → Pipeline hierarchy. |
| `ReadsFromEdgeType` | class | Pipeline reads from a Connection. |
| `SequencesEdgeType` | class | One stage or step must complete before the next begins (ordered sequence). Used for: Stage → Sequences →… |
| `StageNodeType` | class | An ETL project stage node — an ordered phase within a Project. Stage N+1 waits for all Steps of Stage N… |

## Installation

```bash
dotnet add package Fdw.Data.Lineage --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
