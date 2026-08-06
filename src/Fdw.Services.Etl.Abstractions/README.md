# Fdw.Services.Etl.Abstractions

The ETL contracts — pipelines, transforms, mappers and the execution model.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (33)

| Type | Kind | Purpose |
|---|---|---|
| `IAggregateFunction` | interface | Interface for aggregate function type options consumed by Aggregate transforms. |
| `IAggregationSpec` | interface | Read-only surface for a single aggregation within an Aggregate transform request. |
| `ICalculationSpec` | interface | Read-only surface for a single computed column within a Calculate transform request. |
| `IEffectivePolicyResolver` | interface | Resolves the effective (inherited) policy snapshot for a Project, Stage, or Step by walking the parent… |
| `IEtlPipeline` | interface | Represents an ETL pipeline that extracts, transforms, and loads data. The ETL KIND of the general… |
| `IEtlPipelineExecutionResult` | interface | Represents the result of a pipeline execution. |
| `IEtlPipelineFactory` | interface | Marker interface for ETL pipeline factories. |
| `IEtlPipelineFactory<TPipeline, TConfiguration>` | interface | Generic interface for ETL pipeline factories with typed configuration. |
| `IEtlPipelineType` | interface | Non-generic interface for ETL pipeline type definitions. |
| `IEtlPipelineType<TPipeline, TConfiguration, TFactory>` | interface | Generic interface for typed ETL pipeline type definitions. |
| `IEtlPipelineTypedConfiguration` | interface | Typed body for an ETL ENGINE (e.g. BatchCopyPipelineConfiguration, StreamingPipelineConfiguration). The… |
| `IExecutionCompletionSignaler` | interface | Singleton registry that allows the project orchestrator to await pipeline completion without polling.… |
| `IExpressionEvaluator` | interface | Interface for evaluating expressions in ETL transforms. |
| `IFieldMapping` | interface | Represents a field mapping in a Map transform. |
| `IFormulaLanguage` | interface | Interface for formula language type options consumed by Calculate transforms. |
| `ILookupJoinType` | interface | Interface for lookup join type options consumed by Lookup transforms. |

## Base types (16)

| Type | Kind | Purpose |
|---|---|---|
| `AggregateFunctionBase` | class | Base class for aggregate function type options using the CRTP pattern. |
| `AggregateFunctions` | class | Collection of aggregate functions available to Aggregate transforms (Sum, Count, Avg, Min, Max, First,… |
| `FormulaLanguageBase` | class | Base class for formula language type options using the CRTP pattern. |
| `FormulaLanguages` | class | Collection of formula languages available to Calculate transforms. Extensible — a consuming assembly can… |
| `LookupJoinTypeBase` | class | Base class for lookup join type options using the CRTP pattern. |
| `LookupJoinTypes` | class | Collection of lookup join types (Inner, Left) available to Lookup transforms. The runtime dispatches… |
| `OrchestrationNodeTypeBase` | class | Base class for orchestration node types using the CRTP pattern. All values are passed via constructor… |
| `OrchestrationNodeTypes` | class | TypeCollection for orchestration node types. Extensible: external assemblies may register additional… |
| `StageFailurePolicies` | class | Collection of stage failure policy types. Controls behavior when a Stage within a Project fails.… |
| `StageFailurePolicyBase` | class | Base class for stage failure policy types using the CRTP pattern. |

## Models and supporting types (34)

| Type | Kind | Purpose |
|---|---|---|
| `ContinueProjectPolicy` | class | When a Stage fails, continue executing subsequent stages in the Project. The Project itself records… |
| `ContinueStagePolicy` | class | When a Pipeline in a Step fails, continue executing remaining pipelines in the Stage. The Stage itself… |
| `EdgeInspectorState` | class | Per-edge inspector state for a single test execution. All counter fields use operations for thread… |
| `ExecutionPolicySnapshot` | record | Immutable snapshot of the resolved effective policy for a Project, Stage, or Step. All fields are… |
| `HaltProjectPolicy` | class | When a Stage fails, halt the entire Project immediately. All subsequent stages are not executed. This is… |
| `HaltStagePolicy` | class | When a Pipeline in a Step fails, halt the entire Stage immediately. All sibling pipelines in the same… |
| `OrchestrationNodeConfiguration` | class | ManagedConfiguration for the pipe.OrchestrationNode table. Represents a single node in the recursive… |
| `OrchestrationNodePipelineMembershipConfiguration` | class | ManagedConfiguration for the pipe.OrchestrationNodePipeline table. Represents the membership of a… |
| `OrchestrationNodePipelinePrerequisiteConfiguration` | class | ManagedConfiguration for the pipe.OrchestrationNodePipelinePrerequisite table. Records a directed… |
| `PipelineExecutionOptions` | record | Options controlling a single pipeline execution, including test-mode behavior. Defaults to production… |
| `PipelineExecutionRequest` | class | Work item enqueued by endpoints and dequeued by the background execution service. |
| `PipelineTestExecutionState` | class | Per-execution mutable state managed by . |

## Installation

```bash
dotnet add package Fdw.Services.Etl.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.Services.SecretManagers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
