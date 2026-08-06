# Fdw.Services.Scheduling.Components

Headless components for the scheduling domain.

Headless Blazor components for this domain — 2 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `ScheduleDashboardContext` | class | Immutable context for the schedule dashboard widget. |
| `ScheduleTypeProviderLog` | class | MessageLogging methods for ScheduleProvider schedule type loading operations. EventId range: 4280-4289 |
| `ScheduleContext` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.Scheduling.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Results` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.Services.Scheduling.Abstractions` · `Fdw.Services.Scheduling.Clients` · `Fdw.UI.Components` · `Fdw.Web.Clients` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
