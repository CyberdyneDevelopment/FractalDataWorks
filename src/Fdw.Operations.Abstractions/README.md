# Fdw.Operations.Abstractions

The operations contracts — executions, stages, steps, events and the escalation model.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (16)

| Type | Kind | Purpose |
|---|---|---|
| `IActivityType` | interface | Interface for activity types in the timeline. |
| `IConfigurationIssueType` | interface | Interface for configuration issue types. Extends ITypeOption to enable TypeCollection discovery. |
| `IConfigurationPropertyType` | interface | Interface for configuration property types for UI rendering. |
| `IDataflowNodeType` | interface | Interface for dataflow node types. |
| `IEscalationEvaluator` | interface | Service for evaluating and triggering escalation policies. |
| `IEscalationLevel` | interface | Represents a level within an escalation policy. |
| `IEscalationOverrideMode` | interface | Interface for escalation override modes that control how overrides are applied to policies. |
| `IEscalationPolicy` | interface | Represents an escalation policy configuration. |
| `IEscalationService` | interface | Service for managing escalation policies. |
| `IExecutionEvent` | interface | Represents an append-only event in the execution log. |
| `IExecutionItem` | interface | Represents an execution item in the tracking hierarchy. |
| `IExecutionItemType` | interface | Interface for execution item types in the hierarchy. |
| `IExecutionState` | interface | Interface for execution states for nodes. |
| `IExecutionStateType` | interface | Interface for execution state types in the state machine. |
| `IExecutionTracker` | interface | Service for tracking execution of workflows, jobs, and other hierarchical items. |
| `IOperationDispatcher` | interface | Dispatches an accepted operation for execution. Implementations bridge the gap between trigger… |

## Base types (17)

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationIssueTypeBase` | class | Base class for configuration issue types. |
| `EscalationOverrideModeBase` | class | Base class for escalation override modes using the CRTP pattern. Defines how escalation policy overrides… |
| `EscalationOverrideModes` | class | TypeCollection for escalation override modes that control how policy overrides are applied. |
| `ExecutionItemTypeBase` | class | Base class for execution item types using the CRTP pattern. Defines the hierarchy: Workflow → Job →… |
| `ExecutionItemTypes` | class | TypeCollection for execution item types defining the execution hierarchy. Workflow → Job → Stage → Step… |
| `ExecutionStateTypeBase` | class | Base class for execution state types using the CRTP pattern. Defines the state machine: Scheduled →… |
| `ExecutionStateTypes` | class | TypeCollection for execution state types defining the state machine. State transitions: Scheduled →… |
| `OperationsResultCodeBase` | class | Base class for Operations result codes. |
| `OperationsResultCodes` | class | TypeCollection for Operations result codes. Codes use the categorized-number scheme (prefix "OPS"): Id… |
| `ActivityTypeBase` | class | — |

## Models and supporting types (116)

| Type | Kind | Purpose |
|---|---|---|
| `ActivityEntryPayload` | class | Represents an entry in the activity timeline. |
| `AuditFilterRequest` | class | Filter criteria for audit record queries. |
| `AuditRecordPayload` | class | Audit trail record. |
| `CancelledStateType` | class | Cancelled - execution was cancelled before completion. |
| `CompensatingStateType` | class | Compensating - execution is running compensation/rollback logic. |
| `CompletedStateType` | class | Completed - execution finished successfully. |
| `ConfigurationData` | class | Generic container for configuration data values. |
| `ConfigurationInstanceDetailPayload` | class | Detailed configuration instance with property values. |
| `ConfigurationInstanceSummaryPayload` | class | Summary information for a configuration instance. |
| `ConfigurationIssueTypes` | class | TypeCollection for configuration issue types. |
| `ConfigurationPropertyInfo` | class | Metadata for a single configuration property. |
| `ConfigurationTypeDetail` | class | Detailed configuration type metadata including all properties. |

## Installation

```bash
dotnet add package Fdw.Operations.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Web.Clients.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
