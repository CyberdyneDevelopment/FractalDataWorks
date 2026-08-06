# Fdw.UI.Wizard

The multi-step wizard component model.

This package declares 1 interface(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IWizardContext` | interface | Interface for shared wizard navigation and status state. Composed into domain-specific context objects… |

## Types (4)

| Type | Kind | Purpose |
|---|---|---|
| `WizardContext` | class | Default immutable implementation of . Built by during each context rebuild and composed into… |
| `WizardCore<TContext>` | class | Headless state-machine core for wizard flows. Owns step navigation, loading/saving/error state, and… |
| `WizardProviderBase<TContext>` | class | Base class for headless wizard provider components. Owns step navigation, loading/saving/error state,… |
| `WizardProviderLog` | class | MessageLogging for operations. EventId range: 4600-4620 |

## Installation

```bash
dotnet add package Fdw.UI.Wizard --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.UI.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
