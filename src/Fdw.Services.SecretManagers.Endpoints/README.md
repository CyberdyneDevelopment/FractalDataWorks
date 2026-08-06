# Fdw.Services.SecretManagers.Endpoints

Endpoint bases for managing secret-manager configuration.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `CreateSecretManagerEndpointBase` | class | Base endpoint for creating a new secret manager configuration. Route: POST /secret-managers |
| `DeleteSecretManagerEndpointBase` | class | Base endpoint for deleting a secret manager configuration. Route: DELETE /secret-managers/{Name} |
| `GetSecretManagerEndpointBase` | class | Base endpoint for getting a specific secret manager by name. Route: GET /secret-managers/{Name} |
| `ListSecretManagerTypesEndpointBase` | class | Generic base endpoint for listing available secret manager types from the source-generated collection. |
| `ListSecretManagersEndpointBase` | class | Base endpoint for listing all configured secret managers. Route: GET /secret-managers |
| `UpdateSecretManagerEndpointBase` | class | Base endpoint for updating an existing secret manager configuration. Route: PUT /secret-managers/{Name} |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `CreateSecretManagerRequest` | class | Request to create a new secret manager configuration. |
| `DeleteSecretManagerRequest` | class | Request to delete a secret manager by name. |
| `GetSecretManagerRequest` | class | Request to get a secret manager by name. |
| `SecretManagerDetailResponse` | class | Detail DTO for a single secret manager configuration. |
| `SecretManagerSummaryResponse` | class | Response DTO for a secret manager summary. |
| `UpdateSecretManagerRequest` | class | Request to update an existing secret manager configuration. |

## Installation

```bash
dotnet add package Fdw.Services.SecretManagers.Endpoints --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Services.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Validation.FastEndpoints` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
