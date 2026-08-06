# Fdw.UI.Components

The headless component library — state and behaviour without a fixed skin.

Headless Blazor components for this domain — 14 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `ApiErrorContext` | class | Context for rendering API error display. |
| `ApiResultContext` | class | Context for rendering API result display (success or error). |
| `ErrorDisplayContext` | class | Context for rendering a structured error display. |
| `UiProviderLog` | class | MessageLogging methods for UI provider operations. Generic operation-level messages shared across all UI… |

## Installation

```bash
dotnet add package Fdw.UI.Components --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.UI.Abstractions` · `Fdw.UI.Web.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
