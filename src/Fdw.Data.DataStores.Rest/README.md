# Fdw.Data.DataStores.Rest

REST data-store support.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ODataConverters` | class | TypeCollection for OData EDM primitive type converters. Child collection of DataTypeConverters.… |
| `RestDataStoreResultCodes` | class | TypeCollection for REST DataStore result codes. Codes use categorized numbers (Id == EventId == number,… |

## Options (31 declared)

| Type | Kind | Purpose |
|---|---|---|
| `InvalidODataSourceCode` | class | Invalid OData source. |
| `InvalidOpenApiSourceCode` | class | Invalid OpenAPI source. |
| `ODataBinaryConverter` | class | Converts OData EDM Binary to CLR byte[]. |
| `ODataBooleanConverter` | class | Converts OData EDM Boolean to CLR bool. |
| `ODataByteConverter` | class | Converts OData EDM Byte to CLR byte. |
| `ODataConverters` | class | TypeCollection for OData EDM primitive type converters. Child collection of DataTypeConverters.… |
| `ODataDateConverter` | class | Converts OData EDM Date to CLR DateTime. |
| `ODataDateTimeConverter` | class | Converts OData EDM DateTime to CLR DateTime. |
| `ODataDateTimeOffsetConverter` | class | Converts OData EDM DateTimeOffset to CLR DateTimeOffset. |
| `ODataDecimalConverter` | class | Converts OData EDM Decimal to CLR decimal. |
| `ODataDoubleConverter` | class | Converts OData EDM Double to CLR double. |
| `ODataEntitySetPathFailedCode` | class | Failed to create OData EntitySet path. |
| `ODataGuidConverter` | class | Converts OData EDM Guid to CLR Guid. |
| `ODataImportFailedCode` | class | OData import operation failed. |
| `ODataInt16Converter` | class | Converts OData EDM Int16 to CLR short. |
| `ODataInt32Converter` | class | Converts OData EDM Int32 to CLR int. |
| `ODataInt64Converter` | class | Converts OData EDM Int64 to CLR long. |
| `ODataMetadataFetchFailedCode` | class | Failed to fetch OData metadata from endpoint. |

## Installation

```bash
dotnet add package Fdw.Data.DataStores.Rest --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.Builders` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Data.Importers.Abstractions` · `Fdw.Data.JsonSchema` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Connections`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
