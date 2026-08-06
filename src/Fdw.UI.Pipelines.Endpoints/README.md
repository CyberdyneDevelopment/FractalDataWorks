# Fdw.UI.Pipelines.Endpoints

Endpoint bases for the pipeline designer — visual graph CRUD and task-type discovery.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `GetDesignerStepTypesEndpointBase` | class | Base endpoint for retrieving all available pipeline step types from the PipelineStepTypes… |
| `GetDesignerTaskTypesEndpointBase` | class | Base endpoint for retrieving all available task types for the designer palette. Route: GET… |

## Installation

```bash
dotnet add package Fdw.UI.Pipelines.Endpoints --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.UI.Pipelines.Clients.Abstractions` · `Fdw.Web.Endpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
