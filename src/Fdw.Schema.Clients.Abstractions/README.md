# Fdw.Schema.Clients.Abstractions

The payload models for the schema API client.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Payloads

| Type | Kind | Purpose |
|---|---|---|
| `DatabaseColumnPayload` | class | Column from database discovery. |
| `DatabaseSchemaPayload` | class | Schema information from database discovery. |
| `DatabaseTablePayload` | class | Table or view from database discovery. |
| `ExecuteDdlRequestPayload` | class | Request to execute a DDL script. |
| `FieldMappingInputPayload` | class | Input Payload for a field mapping in a save mappings request. |
| `FieldMappingResponsePayload` | class | Response Payload for a persisted field mapping. |
| `ImportSchemaRequestPayload` | class | Request for importing schema into DataStore configuration via the API client. |
| `SaveSourceMappingsPayload` | class | Request for saving field mappings for a DataSet source. |
| `SchemaCapableConnectionPayload` | class | Represents a connection that supports schema discovery. |
| `TableSchemaPayload` | class | Represents a table schema for UI display. |
| `ViewSchemaPayload` | class | Represents a view schema for UI display. |

## Installation

```bash
dotnet add package Fdw.Schema.Clients.Abstractions --prerelease
```

## Dependencies

`Fdw.Services.Data.Clients.Models` · `Fdw.Web.Clients.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
