# Fdw.UI.Themes.Components

Headless components for theme selection and editing.

Headless Blazor components for this domain — 2 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `BlazorThemeContext` | class | Immutable context for the Blazor theme provider. Cascaded to child components so they can read the… |
| `BlazorThemeProviderLog` | class | MessageLogging for BlazorThemeProvider operations. EventId range: 7200-7209 |
| `ThemeContext` | class | — |

## Installation

```bash
dotnet add package Fdw.UI.Themes.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.UI.Components` · `Fdw.UI.Themes.Abstractions` · `Fdw.UI.Themes.Clients`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
