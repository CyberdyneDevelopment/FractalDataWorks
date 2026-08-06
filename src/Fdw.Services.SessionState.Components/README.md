# Fdw.Services.SessionState.Components

Headless components backed by session state.

Headless Blazor components for this domain — 1 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `SessionStateContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries the current session state… |
| `SessionStateProviderLog` | class | MessageLogging for SessionStateProvider component operations. EventId range: 4400-4419. |

## Installation

```bash
dotnet add package Fdw.Services.SessionState.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.SessionState` · `Fdw.Services.SessionState.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
