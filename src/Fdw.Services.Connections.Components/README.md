# Fdw.Services.Connections.Components

Headless components for connection management.

Headless Blazor components for this domain — 5 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `ConnectionContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ConnectionDashboardContext` | class | Immutable context for the connection dashboard widget. |
| `ConnectionEditorContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ConnectionEditorProviderLog` | class | MessageLogging for ConnectionEditorProvider operations. EventId range: 4220-4234 |
| `ConnectionProviderLog` | class | MessageLogging methods for ConnectionProvider operations. Provider-specific messages with domain context… |
| `ConnectionWizardContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ConnectionWizardProviderLog` | class | MessageLogging for ConnectionWizardProvider operations. EventId range: 4235-4255 |

## Installation

```bash
dotnet add package Fdw.Services.Connections.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Results` · `Fdw.Services.Connections.Clients` · `Fdw.Services.SecretManagers.Clients` · `Fdw.UI.Components` · `Fdw.UI.Wizard`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
