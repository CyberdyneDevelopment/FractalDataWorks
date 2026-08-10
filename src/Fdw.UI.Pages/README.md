# Fdw.UI.Pages

The page set built over the headless components.

Headless Blazor components for this domain — 87 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `AgentActionContext` | class | Context passed from to consumer render fragments. Exposes the current state and action callbacks for… |
| `AgentActionProviderLog` | class | MessageLogging methods for AgentActionProvider operations. EventId range: 4140-4149 |
| `ApiKeyContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ApiKeyProviderLog` | class | MessageLogging for ApiKeyProvider operations. EventId range: 4470-4489 |
| `CalculationFormBuilderContext` | class | State and callbacks for the component. All properties are read-only init — the parent provider rebuilds… |
| `CalculationProviderLog` | class | MessageLogging methods for CalculationProvider operations. Provider-specific messages with domain… |
| `CatalogContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `CatalogProviderLog` | class | MessageLogging for CatalogProvider operations. EventId range: 4490-4504 |
| `ConfigurationContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `ConfigurationPageContext` | record | Extended configuration context that wraps the FDW ConfigurationContext and adds type-detail (property… |
| `ConfigurationProviderLog` | class | MessageLogging methods for ConfigurationProvider operations. Provider-specific messages with baked-in… |
| `GlossaryContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `GlossaryProviderLog` | class | MessageLogging for GlossaryProvider operations. EventId range: 4470-4489 |
| `NotificationBellContext` | class | Immutable context object passed to the consumer RenderFragment by . Provides unread count and recent… |
| `NotificationBellProviderLog` | class | MessageLogging methods for the NotificationBellProvider headless component. EventId range: 4240-4249 |
| `NotificationSettingsContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |

## Installation

```bash
dotnet add package Fdw.UI.Pages --prerelease
```

## Dependencies

`Fdw.Agents.Clients` · `Fdw.Configuration` · `Fdw.Data.Components` · `Fdw.Data.DataSets.Abstractions` · `Fdw.Data.UI.Components` · `Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Operations.Components` · `Fdw.Results` · `Fdw.Schema.Components` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authentication.Clients` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Authorization.Clients` · `Fdw.Services.Catalog.Clients` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Clients` · `Fdw.Services.Connections.Components` · `Fdw.Services.Data.Clients` · `Fdw.Services.Etl.Abstractions` · `Fdw.Services.Etl.Projects.Clients` · `Fdw.Services.ExternalIdentityProviders.Abstractions` · `Fdw.Services.Messaging.Clients` · `Fdw.Services.Notifications.Abstractions` · `Fdw.Services.Notifications.Clients` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.Services.Pipelines.Clients` · `Fdw.Services.Pipelines.Components` · `Fdw.Services.Quality.Abstractions` · `Fdw.Services.Quality.Clients` · `Fdw.Services.Scheduling.Components` · `Fdw.Services.SecretManagers.Clients` · `Fdw.Services.SessionState.Components` · `Fdw.Services.Settings.Clients` · `Fdw.Services.Terminal.Abstractions` · `Fdw.Services.Users.Abstractions` · `Fdw.Services.Users.Clients` · `Fdw.UI` · `Fdw.UI.Canvas.Blazor` · `Fdw.UI.Charts.Blazor` · `Fdw.UI.Components` · `Fdw.UI.Pipelines.Clients` · `Fdw.UI.Pipelines.Clients.Models` · `Fdw.UI.Themes.Clients` · `Fdw.Web.Analytics.Components` · `Fdw.Web.Calculations.Clients` · `Fdw.Web.Clients` · `Fdw.Web.Clients.Abstractions` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
