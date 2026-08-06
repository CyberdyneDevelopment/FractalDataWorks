# Fdw.UI.CommandBuilders.Abstractions

Contracts for headless command-builder components — the UI face of the command catalogue.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (3)

| Type | Kind | Purpose |
|---|---|---|
| `ICommandBuilderContext<TCommandSpec>` | interface | Marker interface for any headless command-builder context. Concrete contexts carry command-kind-specific… |
| `ICommandBuilderProvider<TCommandSpec>` | interface | Marker interface for the headless provider Blazor component of a command builder. Implementations expose… |
| `ICommandBuilderSkin` | interface | Descriptor interface implemented by Blazor components that act as visual skins for a specific command… |

## Installation

```bash
dotnet add package Fdw.UI.CommandBuilders.Abstractions --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
