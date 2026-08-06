# Fdw.Web.Endpoints

Cross-domain endpoint bases.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `ClearSessionStateEndpointBase` | class | Base endpoint for clearing all session state entries for the authenticated user. Route: DELETE… |
| `DeleteSessionStateEndpointBase` | class | Base endpoint for deleting a single session state entry for the authenticated user. Route: DELETE… |
| `GetSessionStateEndpointBase` | class | Base endpoint for getting a session state value by key for the authenticated user. Route: GET… |
| `ListSessionStateKeysEndpointBase` | class | Base endpoint for listing all session state keys for the authenticated user. Route: GET /session-state |
| `UpsertSessionStateEndpointBase` | class | Base endpoint for upserting a session state value for the authenticated user. Route: PUT… |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `PaginatedRequest` | class | Base request for paginated endpoints. |
| `PaginatedRequest` | class | Base request for paginated endpoints. |
| `PaginatedResponse<T>` | class | Wrapper for paginated results. |
| `PaginatedResponse<T>` | class | Wrapper for paginated results. |
| `ResourceCreateRequest` | class | Abstract base request for creating a new named resource. Provides the Name property with standard… |
| `ResourceIdRequest` | class | Abstract base request for operations that identify a resource by ID. Used when resources are identified… |
| `ResourceNameRequest` | class | Abstract base request for operations that identify a resource by name. Used for Get, Delete, and other… |
| `ResourceUpdateRequest` | class | Abstract base request for updating an existing named resource. Name identifies the resource (typically… |
| `TriggerOperationRequest` | class | Base request for triggering an operation execution. Provides common properties for all trigger endpoints… |
| `TriggerOperationResponse` | class | Base response for a triggered operation execution. Provides common fields returned from all trigger… |
| `SessionStateEntryResponse` | class | — |
| `SessionStateKeyRequest` | class | — |
| `SessionStateKeysResponse` | class | — |
| `UpsertSessionStateRequest` | class | — |

## Installation

```bash
dotnet add package Fdw.Web.Endpoints --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Services.SessionState.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
