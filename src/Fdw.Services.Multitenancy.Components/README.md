# Fdw.Services.Multitenancy.Components

Headless tenancy components, including the organization picker.

Headless Blazor components for this domain — 2 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `TenantsAdminContext` | class | Immutable context provided by to its render template. Exposes the list of tenants and operations for… |
| `TenantsAdminProviderLog` | class | MessageLogging for TenantsAdminProvider headless component. EventId range: 4274-4283 |

## Installation

```bash
dotnet add package Fdw.Services.Multitenancy.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.Multitenancy.Clients` · `Fdw.UI.Components` · `Fdw.Web.Clients` · `Fdw.Web.Endpoints` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
