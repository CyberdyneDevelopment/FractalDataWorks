# Fdw.Data.DataSets

DataSets — logical views over one or more stores, with fields, sources, joins, filters and caching resolved at read time.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `DataSetSourceMapperTypes` | class | TypeCollection for all data set source mapper type implementations. Mappers extract raw records from… |
| `DataSetsResultCodes` | class | TypeCollection for DataSets domain result codes. Codes use categorized catalog numbers (Code ==… |
| `DurationUnitTypes` | class | TypeCollection of duration unit options available to field transforms. |
| `TimeZoneTypes` | class | TypeCollection of named timezone options available to field transforms. |

## Options (88 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AddDurationFieldTransformer` | class | Adds a fixed duration to a DateTime or DateTimeOffset value. The output type matches the input type. |
| `AggregateColumnNameRequiredCode` | class | An aggregate measure is missing its aggregateColumnName or inputFieldName. Caller-input validation… |
| `AggregateFunctionInvalidCode` | class | An aggregate measure's aggregateFunctionName is not a registered AggregationFunctions member.… |
| `AggregateGroupByEmptyCode` | class | An aggregate measure's groupByFieldNames is empty or contains an empty element. Caller-input validation… |
| `AlaskaTimeZoneType` | class | US Alaska timezone. |
| `ArabianTimeZoneType` | class | Arabian timezone (GST, Dubai, Muscat, UTC+4). |
| `ArgentinaTimeZoneType` | class | Argentina timezone (ART, UTC-3). |
| `ArizonaTimeZoneType` | class | US Arizona timezone (MST, UTC-7, no daylight saving). |
| `AtlanticTimeZoneType` | class | Canada Atlantic timezone (AST/ADT, UTC-4). |
| `AustraliaCentralTimeZoneType` | class | Australia Central timezone (ACST/ACDT, Adelaide, UTC+9:30). |
| `AustraliaEasternTimeZoneType` | class | Australian Eastern Standard Time (AEST/AEDT, UTC+10/+11). |
| `AustraliaWesternTimeZoneType` | class | Australia Western timezone (AWST, Perth, UTC+8). |
| `AverageFieldTransformer` | class | Computes the average of the input value and a second field value from the current record. Both values… |
| `AzoresTimeZoneType` | class | Azores timezone (AZOT/AZOST, UTC-1 with DST). |
| `BangladeshTimeZoneType` | class | Bangladesh timezone (BST, UTC+6). |
| `BoolToStringFieldTransformer` | class | Maps a boolean field value to one of two configured string labels. |
| `BrazilTimeZoneType` | class | Brazil Brasilia timezone (BRT/BRST, UTC-3). |
| `CapeVerdeTimeZoneType` | class | Cape Verde timezone (CVT, UTC-1). |

## Installation

```bash
dotnet add package Fdw.Data.DataSets --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.Data.DataSets.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Schema.Abstractions` · `Fdw.Services.Calculations.Abstractions` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
