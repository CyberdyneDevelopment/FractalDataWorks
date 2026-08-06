# Fdw.Operations.Clients

The API clients for operations — lineage, dataflow and configuration metadata.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Clients

| Type | Kind | Purpose |
|---|---|---|
| `AuditApiClient` | class | API client for audit trail endpoints. |
| `ConfigurationApiClient` | class | API client for configuration type discovery and management. |
| `DataflowApiClient` | class | API client for dataflow, lineage, and impact analysis endpoints. |
| `EscalationApiClient` | class | API client for escalation policy management endpoints. |
| `ExecutionApiClient` | class | API client for execution history and audit trail endpoints. |
| `LineageApiClient` | class | API client for Data Lineage operations. |
| `NotificationPreferencesApiClient` | class | API client for user notification preferences endpoints. |

## Payloads

| Type | Kind | Purpose |
|---|---|---|
| `NotificationPreferencePayload` | class | Payload for a notification preference entry. |

## Installation

```bash
dotnet add package Fdw.Operations.Clients --prerelease
```

## Dependencies

`Fdw.Operations.Abstractions` · `Fdw.Web.Clients` · `Fdw.Web.Endpoints` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
