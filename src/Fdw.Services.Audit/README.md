# Fdw.Services.Audit

Audit trail services.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `AuditServiceTypes` | class | ServiceTypeCollection for audit service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultAuditServiceType` | class | Default audit service type that registers audit services with the dependency injection container. |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Installation

```bash
dotnet add package Fdw.Services.Audit --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Configuration.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services.Abstractions` · `Fdw.Services.Audit.Abstractions` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
