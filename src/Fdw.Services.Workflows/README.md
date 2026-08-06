# Fdw.Services.Workflows

The workflow service domain — workflow engines as a collection of kinds.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `OrchestratedWorkflowResultCodes` | class | TypeCollection for Orchestration.Workflows result codes. EventId range: 5700-5799… |
| `WorkflowResultCodes` | class | TypeCollection of workflow result codes. EventId range: 7850-7869 |
| `WorkflowTypes` | class | TypeCollection for workflow service types. |

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `WorkflowConfiguration` | class | Base configuration class for all workflow types. Generates the parent table workflow.Workflow which… |

## Installation

```bash
dotnet add package Fdw.Services.Workflows --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataSets.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Workflows.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
