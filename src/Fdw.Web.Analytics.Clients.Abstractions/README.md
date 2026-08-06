# Fdw.Web.Analytics.Clients.Abstractions

The payload models for the analytics, profiling and promotion clients.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Payloads

| Type | Kind | Purpose |
|---|---|---|
| `CreatePromotionPayload` | class | Represents a request to create a new promotion between environments. |
| `DataProfilePayload` | class | Represents a data profile for a DataSet, containing row count and profiling metadata. |
| `EnvironmentPayload` | class | Represents a deployment environment available for promotions. |
| `PromotionPayload` | class | Represents a promotion request between two environments. |

## Installation

```bash
dotnet add package Fdw.Web.Analytics.Clients.Abstractions --prerelease
```

## Dependencies

`Fdw.Web.Clients.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
