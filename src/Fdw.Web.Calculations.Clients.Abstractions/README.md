# Fdw.Web.Calculations.Clients.Abstractions

The payload models for the calculation and formula client.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Clients

| Type | Kind | Purpose |
|---|---|---|
| `ICalculationApiClient` | interface | Contract for the calculation API client covering CRUD, execution, formula tooling, and bulk DataSet… |

## Payloads

| Type | Kind | Purpose |
|---|---|---|
| `CalculationDetailPayload` | class | Detailed representation of a calculation definition including formula and metadata. |
| `CalculationSummaryPayload` | class | Summary representation of a calculation definition for list views. |
| `CalculationTypePayload` | class | Represents one entry in the unified calculation catalog (codified + configured), tagged with the source… |
| `DataSetFieldPayload` | class | Represents a field within a DataSet, including its type and constraints. |
| `DataSetFieldsPayload` | class | Groups all fields for a single DataSet, used in bulk field enumeration for formula autocomplete. |
| `FieldInfoPayload` | class | Lightweight field descriptor used in bulk DataSet field enumeration for formula autocomplete. |
| `PeriodComparisonTypePayload` | class | Represents a period comparison type used for time-based calculations. |
| `ValidateFormulaPayload` | class | Request to validate a formula expression against a target DataSet. |
| `WindowedCalculationRequestPayload` | class | Request to execute a windowed calculation. |
| `WindowedCalculationResponsePayload` | class | Response from a windowed calculation execution. |
| `WindowedOrderFieldPayload` | class | Specifies a field and sort direction for windowed calculation ordering. |

## Installation

```bash
dotnet add package Fdw.Web.Calculations.Clients.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Web.Clients.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
