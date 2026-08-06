# Fdw.Data.JsonSchema

JSON Schema inference and mapping to FDW field types.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `JsonSchemaConverters` | class | TypeCollection for JSON Schema data type converters. Child collection of DataTypeConverters. Provides… |

## Options (14 declared)

| Type | Kind | Purpose |
|---|---|---|
| `JsonSchemaArrayConverter` | class | Converts JSON Schema array to CLR String (serialized JSON). |
| `JsonSchemaBooleanConverter` | class | Converts JSON Schema boolean to CLR Boolean. |
| `JsonSchemaConverters` | class | TypeCollection for JSON Schema data type converters. Child collection of DataTypeConverters. Provides… |
| `JsonSchemaIntegerInt32Converter` | class | Converts JSON Schema integer (int32 format) to CLR Int32. |
| `JsonSchemaIntegerInt64Converter` | class | Converts JSON Schema integer (int64 format) to CLR Int64. |
| `JsonSchemaNumberDecimalConverter` | class | Converts JSON Schema number (default/no format) to CLR Decimal. |
| `JsonSchemaNumberDoubleConverter` | class | Converts JSON Schema number (double format) to CLR Double. |
| `JsonSchemaNumberFloatConverter` | class | Converts JSON Schema number (float format) to CLR Single. |
| `JsonSchemaObjectConverter` | class | Converts JSON Schema object to CLR String (serialized JSON). |
| `JsonSchemaStringConverter` | class | Converts JSON Schema string (default/no format) to CLR String. |
| `JsonSchemaStringDateConverter` | class | Converts JSON Schema string (date format) to CLR DateOnly. |
| `JsonSchemaStringDateTimeConverter` | class | Converts JSON Schema string (date-time format) to CLR DateTime. |
| `JsonSchemaStringTimeConverter` | class | Converts JSON Schema string (time format) to CLR TimeOnly. |
| `JsonSchemaStringUuidConverter` | class | Converts JSON Schema string (uuid format) to CLR Guid. |

## Installation

```bash
dotnet add package Fdw.Data.JsonSchema --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.Data.Builders` · `Fdw.Data.Importers.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
