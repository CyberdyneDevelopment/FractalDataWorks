# Fdw.Operations.Components

Headless components for the operations domain.

Headless Blazor components for this domain — 5 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `AuditContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `AuditProviderLog` | class | MessageLogging methods for the AuditProvider headless component. EventId range: 4203-4209 |
| `ExecutionDetailContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ExecutionDetailProviderLog` | class | MessageLogging for ExecutionDetailProvider operations. EventId range: 4210-4219 |
| `OperationsDashboardContext` | class | Immutable context for the operations dashboard widget. |
| `DataflowContext` | class | — |
| `LineageContext` | class | — |

## Installation

```bash
dotnet add package Fdw.Operations.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Results` · `Fdw.Services.Connections.Clients` · `Fdw.Services.Data.Clients` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.UI.Components` · `Fdw.Web.Calculations.Clients`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
