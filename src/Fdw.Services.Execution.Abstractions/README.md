# Fdw.Services.Execution.Abstractions

Execution-tracking contracts shared across the domains that report progress.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IProcess` | interface | Core interface for any process that can be executed in the Fdw system. Represents a unit of work that… |
| `IProcessMetrics` | interface | Performance metrics for process execution. |
| `IProcessResult` | interface | Result of a process operation execution. |
| `IProcessState` | interface | Interface defining the contract for process state enum options. |
| `IProcessType` | interface | Interface defining the contract for process type enum options. |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `ExecutionMessage` | class | Base class for all Execution-related messages. |
| `ExecutionMessageCollectionBase` | class | Collection definition to generate ExecutionMessages static class. |
| `ProcessStateBase` | class | Base class for process states. States represent the current condition of a process during its lifecycle.… |
| `ProcessTypeBase` | class | Base class for all process types in the Fdw system. Process types define what kinds of work can be… |
| `ProcessTypes` | class | Global collection of all process types across all assemblies. This uses the TypeCollection pattern to… |

## Models and supporting types (21)

| Type | Kind | Purpose |
|---|---|---|
| `Cancelled` | class | Final state when a process has been cancelled before completion. |
| `Completed` | class | Final state when a process has completed successfully. |
| `Created` | class | Initial state when a process is first created but not yet started. |
| `Failed` | class | Final state when a process has failed due to an error. |
| `OperationExecutionCompletedMessage` | class | CurrentMessage indicating that an operation execution has completed successfully. |
| `OperationExecutionFailedMessage` | class | CurrentMessage indicating that an operation execution has failed. |
| `OperationExecutionStartedMessage` | class | CurrentMessage indicating that an operation execution has started. |
| `OperationNotSupportedMessage` | class | CurrentMessage indicating that a requested operation is not supported by the process. |
| `Pending` | class | The process has been triggered but has not yet started execution. |
| `ProcessCancellationRequestedMessage` | class | CurrentMessage indicating that a process cancellation was requested. |
| `ProcessConfigurationInvalidMessage` | class | CurrentMessage indicating that process configuration is invalid. |
| `ProcessConfigurationMissingMessage` | class | CurrentMessage indicating that required process configuration is missing. |

## Installation

```bash
dotnet add package Fdw.Services.Execution.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
