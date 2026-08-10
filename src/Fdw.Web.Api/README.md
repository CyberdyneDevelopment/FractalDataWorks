# Fdw.Web.Api

The shared API surface: endpoint bases, request and response models, and the conventions every FDW HTTP host follows.

This package declares 178 model(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `GetPipelineTypesEndpointBase` | class | Base endpoint for retrieving all registered ETL pipeline engine types from the EtlPipelineTypes… |

## Records (178)

| Type | Kind | Purpose |
|---|---|---|
| `AddDataStoreContainerRequest` | class | Request DTO for adding a container to an existing data store path. |
| `AggregationDto` | class | Response DTO surfacing the Aggregate-transform parameters on a pipeline detail response, read from the… |
| `AggregationItemDto` | class | Response DTO for a single aggregation within an . |
| `AggregationItemRequest` | class | Request body for a single aggregation within an . Maps onto one… |
| `AggregationRequest` | class | Request body for the Aggregate-transform parameters on a create/update pipeline transform. Maps onto… |
| `AssignRoleRequest` | class | Request DTO for assigning a role to a user. |
| `BulkPipelineStatusResponse` | class | Response for bulk pipeline status. |
| `CalculationDto` | class | Response DTO surfacing the Calculate-transform parameters on a pipeline detail response, read from the… |
| `CalculationRequest` | class | Request body for the Calculate-transform parameters on a create/update pipeline transform. Maps onto the… |
| `CompareEnvironmentsRequest` | class | Request to compare configuration between two environments. |
| `ComputedColumnDto` | class | Response DTO for a single computed column within a . |
| `ComputedColumnRequest` | class | Request body for a single computed column within a . Maps onto one… |

## Types (180)

| Type | Kind | Purpose |
|---|---|---|
| `AddDataStoreContainerEndpointBase` | class | Generic base endpoint for adding a container to an existing data store path. POST… |
| `ApiEndpointOptions` | class | Configuration options for API endpoint routing, authorization policies, and domain filtering. |
| `ApprovePromotionEndpointBase` | class | Abstract endpoint that approves a promotion request. |
| `AssignUserRoleEndpointBase` | class | Generic base endpoint for assigning a role to a user. The role assignment is executed in a transaction… |
| `AuthenticationEndpointLog` | class | MessageLogging for authentication endpoint operations. EventId range: 7110-7130 |
| `AuthorizationEndpointLog` | class | High-performance MessageLogging for authorization endpoint operations. EventId range: 3113-3130 |
| `BulkPipelineStatusEndpointBase` | class | Endpoint to get status of all pipelines in a single call. Solves N+1 problem for dashboard and nav menu. |
| `ChangePasswordEndpointBase` | class | Abstract base class for changing the current user's password (POST /users/me/password). |
| `CompareEnvironmentsEndpointBase` | class | Abstract endpoint that compares configuration between two environments for a given entity. |
| `ConnectionEndpointLog` | class | Message logging for connection endpoint operations. EventId range: 7119-7149 |
| `CreateAgentKeyEndpointBase` | class | Abstract base class for creating an agent key (POST /agent-keys). The raw key value is returned exactly… |
| `CreateConnectionEndpointBase<TConfig>` | class | Generic base endpoint for creating a new connection configuration. Composes the whole aggregate — the… |

## Installation

```bash
dotnet add package Fdw.Web.Api --prerelease
```

## Dependencies

`Fdw.Calculations.Aggregations` · `Fdw.Commands.Data` · `Fdw.Data.Abstractions` · `Fdw.Data.DataSets` · `Fdw.Data.DataSets.Abstractions` · `Fdw.Hosting` · `Fdw.Hosting.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Operations.Abstractions` · `Fdw.Operations.Endpoints` · `Fdw.Results` · `Fdw.Schema.Abstractions` · `Fdw.Schema.Clients.Abstractions` · `Fdw.Security.Hashing` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Catalog.Clients.Models` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Data.Clients.Models` · `Fdw.Services.Etl` · `Fdw.Services.Etl.Abstractions` · `Fdw.Services.ExternalIdentityProviders` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.Pipelines` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.Services.Quality` · `Fdw.Services.Scheduling` · `Fdw.Services.Scheduling.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Services.Users` · `Fdw.Services.Users.Abstractions` · `Fdw.UI.Themes` · `Fdw.Validation.FastEndpoints` · `Fdw.Web.Endpoints` · `Fdw.Web.RestEndpoints` · `Fdw.Web.Search.Clients.Models`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators` · `Fdw.Registration.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
