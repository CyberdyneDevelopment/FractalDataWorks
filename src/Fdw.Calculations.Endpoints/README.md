# Fdw.Calculations.Endpoints

Endpoint bases for calculation operations.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `ComputeGroupedStatSetEndpointBase` | class | Base endpoint for computing grouped statistical summaries (StatSet) with dimensions. |
| `ComputeStatSetEndpointBase` | class | Base endpoint for computing statistical summaries (StatSet) for specified columns. |
| `CreateCalculationEntityEndpointBase` | class | Base endpoint for creating a calculation entity. Route: POST /calculation-entities |
| `DeleteCalculationEntityEndpointBase` | class | Base endpoint for deleting a calculation entity (soft delete). Route: DELETE /calculation-entities/{id} |
| `ExecuteCalculationEndpointBase` | class | Base endpoint for executing a calculation on provided values. |
| `ExecuteCalculationEntityEndpointBase` | class | Base endpoint for executing a calculation entity. Route: POST /calculation-entities/{id}/execute |
| `GetCalculationEntityEndpointBase` | class | Base endpoint for getting a calculation entity by ID. Route: GET /calculation-entities/{id} |
| `ListCalculationEntitiesEndpointBase` | class | Base endpoint for listing calculation entities. Route: GET /calculation-entities |
| `ListCalculationTypesEndpointBase` | class | Base endpoint for listing all available calculation types — the unified catalog (codified + configured)… |
| `ListPeriodComparisonTypesEndpointBase` | class | Base endpoint for listing all available period comparison types. |
| `ListVisualizationTypesEndpointBase` | class | Base endpoint for listing all available visualization types. |
| `PreviewCalculationEndpointBase` | class | Base endpoint for previewing a calculation with sample data. |
| `UpdateCalculationEntityEndpointBase` | class | Base endpoint for updating a calculation entity. Route: PUT /calculation-entities/{id} |
| `WindowedCalculationEndpointBase` | class | Base endpoint for executing a windowed calculation. Route: POST /calculations/windowed |
| `ListWindowFunctionsEndpointBase` | class | Base endpoint for listing the window functions a windowed calculation may name. Route: GET /calculations/window-functions |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `CalculationEntityIdRequest` | class | Request with a calculation entity ID. |
| `CreateCalculationEntityRequest` | class | Request to create a new calculation entity. |
| `ExecuteCalculationEntityRequest` | class | Request to execute a calculation entity. |
| `ExecuteCalculationEntityResponse` | class | Response from executing a calculation entity. |
| `UpdateCalculationEntityRequest` | class | Request to update an existing calculation entity. |
| `WindowFunctionPayload` | class | One window function a windowed calculation may name. |
| `WindowFunctionsResponse` | class | The window functions a windowed calculation may name. |
| `WindowedCalculationRequest` | class | Request to execute a windowed calculation. |
| `WindowedCalculationResponse` | class | Response from a windowed calculation execution. |
| `WindowedOrderFieldResponse` | class | Specifies a field and sort direction for windowed calculation ordering. |

## Installation

```bash
dotnet add package Fdw.Calculations.Endpoints --prerelease
```

## Dependencies

`Fdw.Calculations` · `Fdw.Calculations.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Calculations` · `Fdw.Services.Calculations.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.Workspaces` · `Fdw.Validation.FastEndpoints` · `Fdw.Web.Calculations.Clients.Abstractions` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; Dependencies checked against the csproj on 2026-10-25 -->
