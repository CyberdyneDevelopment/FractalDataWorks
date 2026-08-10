# Fdw.Services.Data.Clients.Models

The payload models, edit models and validators for the data API client.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Payloads

| Type | Kind | Purpose |
|---|---|---|
| `AddDataStoreContainerPayload` | class | Request body for adding a container to an existing data store path. |
| `ColumnSchemaPayload` | class | Represents a column schema for UI display. |
| `CreateDataSetPayload` | class | Request to create a new DataSet. |
| `CreateDataSetSourcePayload` | class | Request to create a DataSet source. |
| `DataPreviewRequestPayload` | class | Request for data preview. |
| `DataPreviewResponsePayload` | class | Response for data preview. |
| `DataSetAggregationEditorPayload` | class | Editor-state payload for an aggregation definition during in-place workbench composition. |
| `DataSetCachingPayload` | class | Represents caching configuration for a DataSet. |
| `DataSetCalculationEditorPayload` | class | Editor-state payload for a calculated field during in-place workbench composition. |
| `DataSetCompositionOperationPayload` | class | Payload for API requests that add or remove sources, joins, calculations, and aggregations during… |
| `DataSetDetailPayload` | class | Detailed information for a DataSet. |
| `DataSetFieldMappingPayload` | class | Represents a field mapping between DataSet and source. |
| `DataSetFieldPayload` | class | Represents a field in a DataSet. |
| `DataSetFilterConditionPayload` | class | A filter condition applied to a DataSet query or data preview. |
| `DataSetJoinEditorPayload` | class | Editor-state payload for a join between two DataSet sources during in-place workbench composition. |
| `DataSetJoinPayload` | class | Represents a join between two sources in a DataSet. |

## Installation

```bash
dotnet add package Fdw.Services.Data.Clients.Models --prerelease
```

## Dependencies

`Fdw.Web.Clients.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
