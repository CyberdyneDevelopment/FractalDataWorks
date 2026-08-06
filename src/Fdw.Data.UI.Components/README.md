# Fdw.Data.UI.Components

Skinned data components for a headless UI.

Headless Blazor components for this domain — 29 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `DataCommandContext` | class | State shape for the DataCommandProvider headless component. Passed to command-kind skin components via… |
| `DataCommandProviderLog` | class | MessageLogging for DataCommandProvider operations. EventId range: 4300-4319 |
| `DataPreviewPageContext` | class | Context exposed by to its child content. |
| `DataSetDetailPreviewContext` | class | Context exposed by to its child content. Carries the current preview state and action callbacks for the… |

## Installation

```bash
dotnet add package Fdw.Data.UI.Components --prerelease
```

## Dependencies

`Fdw.Calculations.Abstractions` · `Fdw.Data` · `Fdw.Data.Components` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Schema.Components` · `Fdw.Services.Calculations` · `Fdw.Services.Calculations.Abstractions` · `Fdw.Services.Data.Clients` · `Fdw.Services.SessionState.Components` · `Fdw.UI.CommandBuilders.Abstractions` · `Fdw.UI.Components`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
