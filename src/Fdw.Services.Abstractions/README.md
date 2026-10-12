# Fdw.Services.Abstractions

`ServiceTypeBase` and the service-type contracts — what a kind IS (service, factory and configuration types) plus the three replaceable phase bodies that register it.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `HealthStates` | class | Collection of health state TypeOptions. |
| `IServiceOption` | interface | Marks a service interface as the ServiceInterface of a [ServiceTypeCollection] — i.e. a… |
| `ServiceTypeCollectionDescriptor` | record | Default record produced by the opt-in Fdw.Services.Registration.SourceGenerators generator for each… |

## Options (0 declared)

| Type | Kind | Purpose |
|---|---|---|
| `IHealthMonitorFactory<TService, TConfiguration>` | interface | Factory contract for creating instances from a typed configuration. One factory per registered… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Installation

```bash
dotnet add package Fdw.Services.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
