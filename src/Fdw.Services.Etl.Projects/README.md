# Fdw.Services.Etl.Projects

ETL project definitions and their orchestration types.

This package declares 1 interface(s), 2 service/provider type(s), 5 model(s).

## Options (4)

| Type | Kind | Purpose |
|---|---|---|
| `OrchestrationNodeConfigurationCommand` | class | ConfigurationCommands TypeOption for the OrchestrationNode configuration domain. Targets the… |
| `ProjectConfigurationCommand` | class | ConfigurationCommands TypeOption for the Project configuration domain. Targets the pipe.Project table. |
| `StageConfigurationCommand` | class | ConfigurationCommands TypeOption for the Stage configuration domain. Targets the pipe.ProjectStage table. |
| `StepConfigurationCommand` | class | ConfigurationCommands TypeOption for the Step configuration domain. Targets the pipe.StageStep table. |

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IOrchestrationNodeOrchestrator` | interface | Executes an orchestration node tree recursively. Branch nodes (CanHostPipelines=false) execute children… |

## Services (2)

| Type | Kind | Purpose |
|---|---|---|
| `OrchestrationNodeConfigurationProvider` | class | Configuration provider for OrchestrationNode — the blessed self-referencing-tree carve-out. |
| `OrchestrationNodeOrchestratorBackgroundService` | class | Background service that dequeues orchestration node execution requests and dispatches them to in… |

## Records (5)

| Type | Kind | Purpose |
|---|---|---|
| `OrchestrationNodeExecutionRequest` | class | Work item enqueued by endpoints and dequeued by the orchestrator background service. Uses the root… |
| `OrchestrationNodeLineageRecord` | class | Internal query record for reading pipe.OrchestrationNode rows for the lineage graph. Replaces the v1… |
| `OrchestrationNodePipelineLineageRecord` | class | Internal query record for reading pipe.OrchestrationNodePipeline rows for the lineage graph. Represents… |
| `OrchestrationNodePipelinePrerequisiteLineageRecord` | class | Internal query record for reading pipe.OrchestrationNodePipelinePrerequisite rows for the lineage graph.… |
| `ProjectExecutionRequest` | class | Work item enqueued by endpoints and dequeued by the project orchestrator background service. |

## Types (19)

| Type | Kind | Purpose |
|---|---|---|
| `EffectivePolicyResolver` | class | Resolves the effective (fully-inherited) policy snapshot for a Project, Stage, or Step by walking the… |
| `ExecutionCompletionSignaler` | class | Singleton TCS registry allowing the project orchestrator to await pipeline completion without polling.… |
| `ExecutionPolicyElevationValidator<T>` | class | Reusable FluentValidation validator that applies policy elevation rules from a parent effective… |
| `OrchestrationNodeConfigurationLog` | class | MessageLogging for ETL orchestration node configuration operations. EventId range: 6532–6565 (reuses… |
| `OrchestrationNodeConfigurationValidator` | class | FluentValidation validator for . Validates name, NodeTypeId, parent constraints, policy fields, and… |
| `OrchestrationNodeConfigurationWriter` | class | Validates and persists records. Calls FluentValidation before persist, and invalidates the… |
| `OrchestrationNodeExecutionQueue` | class | Channel-based bounded queue for orchestration node execution requests. Provides backpressure by… |
| `OrchestrationNodeOrchestrator` | class | Recursive orchestration engine for the OrchestrationNode hierarchy. Branch nodes… |
| `OrchestrationNodeOrchestratorLog` | class | MessageLogging for OrchestrationNode orchestrator operations. EventId range: 8180-8199. |
| `OrchestrationTypes` | class | Three-phase DI registration for ETL project orchestration services. Registers the OrchestrationNode… |
| `PolicyElevationValidator` | class | Stateless implementation of . Enforces the rule that child policy fields can only be equal to or… |
| `ProjectConfigurationLog` | class | MessageLogging for ETL project orchestration configuration operations. EventId range: 6532–6565 (extends… |

## Installation

```bash
dotnet add package Fdw.Services.Etl.Projects --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data` · `Fdw.Data.Lineage` · `Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Operations.Endpoints` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Etl` · `Fdw.Services.Etl.Abstractions` · `Fdw.Services.Pipelines` · `Fdw.Services.Resiliency`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
