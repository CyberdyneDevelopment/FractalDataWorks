# Fdw.Services.Workflows.Abstractions

The workflow contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (9)

| Type | Kind | Purpose |
|---|---|---|
| `IGenericWorkflow` | interface | Service interface for workflow execution and management. |
| `IGenericWorkflow<TConfiguration>` | interface | Typed workflow service interface with configuration. |
| `IWorkflowFactory` | interface | Marker interface for workflow factories. |
| `IWorkflowFactory<TService, TConfiguration>` | interface | Generic interface for workflow factories with typed configuration. |
| `IWorkflowProvider` | interface | Provides centralized registry and resolution for Workflow configurations. Supports both… |
| `IWorkflowServiceExecutionContext` | interface | Service-layer execution context for workflow operations. |
| `IWorkflowStepContext` | interface | Context for a workflow step execution. |
| `IWorkflowType` | interface | Non-generic interface for workflow service types. |
| `IWorkflowType<TService, TConfiguration, TFactory>` | interface | Interface for workflow service types. |

## Installation

```bash
dotnet add package Fdw.Services.Workflows.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
