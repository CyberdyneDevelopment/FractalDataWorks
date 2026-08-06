# Fdw.Services.Pipelines.Components

Headless components for the pipeline domain.

Headless Blazor components for this domain — 5 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `PipelineBuilderContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `PipelineBuilderProviderLog` | class | MessageLogging for PipelineBuilderProvider operations. EventId range: 4250-4265 |
| `PipelineCanvasEditContext` | class | In-memory edit context for a . |
| `PipelineDashboardContext` | class | Immutable context for the pipeline dashboard widget. |
| `PipelineStepTypeProviderLog` | class | MessageLogging methods for PipelineBuilderProvider step type loading operations. EventId range: 4266-4275 |
| `PipelineWizardContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `SchedulePipelineContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `SchedulePipelineProviderLog` | class | MessageLogging for SchedulePipelineProvider operations. EventId range: 4270-4279 |
| `PipelineContext` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.Pipelines.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Results` · `Fdw.Services.Connections.Clients` · `Fdw.Services.Data.Clients` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.UI.Abstractions` · `Fdw.UI.Components` · `Fdw.UI.Pipelines.Clients.Abstractions` · `Fdw.UI.Wizard`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
