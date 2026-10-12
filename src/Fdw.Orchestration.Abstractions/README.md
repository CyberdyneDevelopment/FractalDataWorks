# Fdw.Orchestration.Abstractions

Orchestration contracts — the atomic/composite command pair, workflow steps and the compensation model.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (57)

| Type | Kind | Purpose |
|---|---|---|
| `IAtomicCommand` | interface | Base abstraction for atomic (single-operation) commands. These are commands that perform a single,… |
| `IBackoffStrategy` | interface | Interface for backoff strategy TypeOptions. |
| `ICachePriority` | interface | Interface for cache entry priority levels. |
| `ICommand` | interface | Base abstraction for all executable commands in the platform. All operations (atomic or composite)… |
| `ICompensationError` | interface | Represents a compensation error. |
| `ICompensationHandler` | interface | Handles compensation logic when workflows fail. |
| `ICompensationResult` | interface | Result of compensation execution. |
| `ICompositeCommand` | interface | Base abstraction for composite (multi-step) commands. These are commands that orchestrate multiple… |
| `ICurrentWorkflowStatus` | interface | Current status of a workflow execution. |
| `IDefinitionCache` | interface | Cache for orchestration definitions. |
| `IDependencyResolver` | interface | Resolves dependencies between workflow steps and pipelines. |
| `IErrorHandlingMode` | interface | Interface for error handling mode TypeOptions. |
| `IExecutionContext` | interface | Base execution context shared by every execution-scope domain context. |
| `IExecutionGroup` | interface | Represents a group of steps that can execute in parallel. |
| `IExecutionPolicyContext` | interface | Policy context — the rules that govern a single execution run. Composed under so the executor reads… |
| `IExecutionStatus` | interface | Interface for execution status TypeOptions. |

## Base types (14)

| Type | Kind | Purpose |
|---|---|---|
| `BackoffStrategyBase` | class | Base class for backoff strategy TypeOptions. |
| `CachePriorities` | class | TypeCollection for cache entry priority levels. |
| `CachePriorityBase` | class | Base class for cache entry priority levels. |
| `ErrorHandlingModeBase` | class | Base class for error handling mode TypeOptions. |
| `ExecutionStatusBase` | class | Base class for execution status TypeOptions. |
| `OrchestrationResultCodeBase` | class | Base class for orchestration result codes. |
| `OrchestrationResultCodes` | class | TypeCollection for orchestration result codes. Codes use categorized numbers (ORCH prefix):… |
| `StageTypeBase` | class | Base class for pipeline stage type TypeOptions. |
| `ValidationRuleTypeBase` | class | Base class for validation rule type TypeOptions. |
| `ValidationSeverityBase` | class | Base class for validation severity TypeOptions. |

## Models and supporting types (46)

| Type | Kind | Purpose |
|---|---|---|
| `AlwaysConditionType` | class | Always run (no condition). |
| `BackoffStrategies` | class | TypeCollection for backoff strategies. |
| `CacheEntryOptions` | class | Options for cache entries. |
| `CancelledExecutionStatus` | class | Workflow was cancelled. |
| `CompensatingExecutionStatus` | class | Workflow is being compensated. |
| `CustomStepType` | class | Execute a custom action. |
| `DecisionStepType` | class | Execute a decision/branch. |
| `ErrorHandlingModes` | class | TypeCollection for error handling modes. |
| `ExecutionFailedCode` | class | Orchestration execution failed. |
| `ExecutionPolicyContext` | class | Default implementation of . Immutable after construction; populated via object initializer. |
| `ExecutionState` | class | Represents the persisted state of an orchestration execution. |
| `ExecutionStatuses` | class | TypeCollection for execution statuses. |

## Installation

```bash
dotnet add package Fdw.Orchestration.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
