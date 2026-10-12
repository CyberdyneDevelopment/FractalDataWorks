# Fdw.Services.Pipelines

The pipeline service domain — the kinds of pipeline FDW can run and the provider that builds one from configuration.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PipelineServiceTypes` | class | ServiceTypeCollection for the pipeline-service domain (gateway-backed pipeline configuration provider).… |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultPipelineServiceType` | class | Default pipeline-service type. Registers the gateway-backed that the pipeline endpoints depend on. |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `PipelineConfiguration` | class | General header configuration for pipeline services representing the pipe.Pipeline parent table. The… |

## Installation

```bash
dotnet add package Fdw.Services.Pipelines --prerelease
```

## Dependencies

`Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Messages` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.SignalR` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
