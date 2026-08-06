# Fdw.Collections

TypeCollections — FDW's extensible enum. The attributes, the CRTP base types, `ServiceTypeCollectionBase` with its three startup phases, and the not-found sentinel that removes null from every lookup.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PlatformServiceProviderAttribute` | class | Marks a hand-written three-phase class (declaring static Configure&lt;TBuilder&gt;(TBuilder,… |
| `TypeCollectionFactoryAttribute` | class | Marks a class as a factory collection that generates factory methods for creating command/option… |

## Installation

```bash
dotnet add package Fdw.Collections --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
