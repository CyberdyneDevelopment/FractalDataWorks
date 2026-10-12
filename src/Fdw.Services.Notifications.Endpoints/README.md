# Fdw.Services.Notifications.Endpoints

Endpoint bases for notification operations.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `GetNotificationEndpointBase` | class | Base endpoint for getting a notification configuration by name. |
| `ListNotificationListsEndpointBase` | class | Base endpoint for listing all notification lists (recipient groups). |
| `ListNotificationRulesEndpointBase` | class | Base endpoint for listing all notification rules. |
| `ListNotificationsEndpointBase` | class | Base endpoint for listing all notification configurations. |
| `ListUserPreferencesEndpointBase` | class | Base endpoint for getting user notification preferences. Returns default preferences when no persisted… |
| `UpdateUserPreferencesEndpointBase` | class | Base endpoint for updating user notification preferences. |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `NotificationNameRequest` | class | Request DTO that identifies a notification by name. |
| `UpdateUserPreferencesRequest` | class | Request DTO for updating user notification preferences. |
| `UserPreferencesRequest` | class | Request DTO that identifies a user for notification preferences. |

## Installation

```bash
dotnet add package Fdw.Services.Notifications.Endpoints --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Notifications` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
