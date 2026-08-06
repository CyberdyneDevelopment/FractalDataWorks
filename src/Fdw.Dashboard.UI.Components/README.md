# Fdw.Dashboard.UI.Components

Dashboard components for a headless skin — the cross-domain context types a dashboard composes.

Headless Blazor components for this domain — 3 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Installation

```bash
dotnet add package Fdw.Dashboard.UI.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Components` · `Fdw.Results` · `Fdw.Services.Connections.Components` · `Fdw.Services.Pipelines.Components` · `Fdw.Services.Scheduling.Components` · `Fdw.UI.Components` · `Fdw.UI.Themes` · `Fdw.Web.Analytics.Components`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
