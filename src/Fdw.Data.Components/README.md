# Fdw.Data.Components

Headless components for the data domain — stores, sets, fields and the command builder over them.

Headless Blazor components for this domain — 17 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `AnnotationContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `AnnotationProviderLog` | class | MessageLogging methods for AnnotationProvider operations. Provider-specific messages with domain context… |
| `CalculatedDataSetContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `CalculatedDataSetProviderLog` | class | MessageLogging for CalculatedDataSetProvider operations. EventId ranges: 4180-4189 (pipeline ops),… |
| `ClientsDataStoreConfigurationProvider` | class | The UI-side for — feeds ConfiguredDataStoreProvider (Fdw.Data.DataNodes) from instead of a gateway.… |
| `ConfigurationDrillDownContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries tree state, configuration… |
| `ConfigurationDrillDownProvider` | class | Headless provider that discovers tree structure from configuration metadata and walks a pre-loaded root… |
| `ConfigurationDrillDownProviderLog` | class | MessageLogging methods for operations. EventId range: 4650-4670 |
| `DataMapperContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `DataMapperProviderLog` | class | MessageLogging for DataMapperProvider operations. EventId range: 1800-1809 |
| `DataPreviewContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries state snapshots for… |
| `DataPreviewProviderLog` | class | MessageLogging for DataPreviewProvider operations. EventId range: 1810-1824 |
| `DataSetContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `DataSetDetailContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries working-set state and… |
| `DataSetDetailProviderLog` | class | MessageLogging methods for DataSetDetailProvider operations. EventId range: 9700-9729 |
| `DataSetProviderLog` | class | MessageLogging methods for DataSetProvider operations. Provider-specific messages with domain context… |

## Installation

```bash
dotnet add package Fdw.Data.Components --prerelease
```

## Dependencies

`Fdw.Data.Configuration` · `Fdw.Data.DataNodes` · `Fdw.MessageLogging.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Operations.Clients` · `Fdw.Schema.Clients` · `Fdw.Services.Catalog.Clients` · `Fdw.Services.Connections.Clients` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Data.Clients` · `Fdw.UI.Abstractions` · `Fdw.UI.Components` · `Fdw.UI.Pipelines.Clients.Abstractions` · `Fdw.UI.Wizard` · `Fdw.Web.Calculations.Clients`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
