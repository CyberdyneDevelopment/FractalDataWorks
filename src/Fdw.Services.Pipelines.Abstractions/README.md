# Fdw.Services.Pipelines.Abstractions

The pipeline contracts and configuration model.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (9)

| Type | Kind | Purpose |
|---|---|---|
| `IDataDestinationKind` | interface | Interface for data destination kind types. |
| `IDataSourceKind` | interface | Interface for data source kind types. |
| `IPipeline` | interface | General runtime base for any pipeline KIND (ETL today; other kinds in future). Carries the… |
| `IPipelineClient` | interface | Defines the contract for interacting with the pipeline configuration API. |
| `IPipelineJobClient` | interface | Defines the contract for interacting with the pipeline job service. |
| `IPipelineStepType` | interface | Represents a pipeline step type that defines the role and configuration requirements of a step. |
| `IPipelineTaskType` | interface | Represents a pipeline task category type that defines the role a task node plays in the designer canvas… |
| `IPipelineTypedConfiguration` | interface | Marker interface for a pipeline KIND typed body (e.g. EtlPipelineConfiguration). The general… |
| `IWriteMode` | interface | Interface for write mode types. |

## Base types (8)

| Type | Kind | Purpose |
|---|---|---|
| `DataDestinationKindBase` | class | Base class for data destination kind types using CRTP pattern. |
| `DataDestinationKinds` | class | Collection of data destination kind types. |
| `DataSourceKindBase` | class | Base class for data source kind types using CRTP pattern. |
| `DataSourceKinds` | class | Collection of data source kind types. |
| `PipelineStepTypeBase` | class | Base class for pipeline step types using the CRTP pattern. |
| `PipelineTaskTypeBase` | class | Base class for pipeline task type definitions using the CRTP pattern. |
| `WriteModeBase` | class | Base class for write mode types using CRTP pattern. |
| `WriteModes` | class | Collection of write mode types. |

## Models and supporting types (49)

| Type | Kind | Purpose |
|---|---|---|
| `AggregationClientRequest` | class | Client-side request for the Aggregate-transform parameters on a create/update pipeline transform. Field… |
| `AggregationItemClientRequest` | class | Client-side request for a single aggregation within an . Field names mirror the server's… |
| `AppendMode` | class | Append data to existing data. |
| `BranchStepType` | class | Pipeline step type for conditional branching. |
| `CalculationClientRequest` | class | Client-side request for the Calculate-transform parameters on a create/update pipeline transform. Field… |
| `ColumnDisposal` | class | Specifies columns to be dropped/discarded after a task completes. This is a memory optimization that… |
| `ComputedColumnClientRequest` | class | Client-side request for a single computed column within a . Field names mirror the server's… |
| `ConditionalTaskType` | class | Pipeline task type for branching the data flow based on a condition. |
| `ConnectionDestinationKind` | class | Write directly to a connection (ETL pattern). |
| `ConnectionKind` | class | Direct connection (ETL pattern) - reads directly from a physical connection. |
| `CreatePipelineClientRequest` | class | Client-side request to create a new pipeline. |
| `DataDestinationReference` | class | Framework-agnostic reference to a data destination. Can be a Connection (ETL) or DataSet (ELT). |

## Installation

```bash
dotnet add package Fdw.Services.Pipelines.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Transformations.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
