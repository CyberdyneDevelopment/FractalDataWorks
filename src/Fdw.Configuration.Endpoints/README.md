# Fdw.Configuration.Endpoints

Endpoint bases for managing configuration instances.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `GetConfigurationInstanceEndpointBase` | class | Base endpoint for getting a specific configuration instance with all values. |
| `ListConfigurationInstancesEndpointBase` | class | Base endpoint for listing configuration instances with optional category filter. |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationInstanceDetailResponse` | class | Detailed configuration instance with property values. |
| `ConfigurationInstanceSummaryResponse` | class | Summary information for a configuration instance. |
| `GetConfigurationInstanceRequest` | class | Request for getting a specific configuration instance. |
| `ListConfigurationInstancesRequest` | class | Request for listing configuration instances. |

## Installation

```bash
dotnet add package Fdw.Configuration.Endpoints --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
