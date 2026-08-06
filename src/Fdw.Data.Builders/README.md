# Fdw.Data.Builders

Builders that assemble a DataStore's node tree from configuration.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `BuilderResultCodes` | class | TypeCollection for Data.Builders result codes. EventId range: 6250-6299 |

## Options (27 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ContainerTypeRequiredCode` | class | Container type is required. |
| `DatasetDuplicateFieldsCode` | class | Dataset has duplicate field names. |
| `DatasetInvalidKeyFieldsCode` | class | Dataset has key fields that don't exist in the field list. |
| `DatasetMissingFieldsCode` | class | Dataset must have at least one field. |
| `DatasetMissingKeyFieldsCode` | class | Dataset must have at least one key field. |
| `DatasetNameRequiredCode` | class | Dataset name is required. |
| `FieldInvalidMaxLengthCode` | class | MaxLength must be greater than zero. |
| `FieldNameRequiredCode` | class | Field name is required. |
| `FieldTypeRequiredCode` | class | Field type is required. |
| `ParameterDefaultTypeMismatchCode` | class | Default value type is not compatible with parameter type. |
| `ParameterMissingCode` | class | Required parameter is missing. |
| `ParameterNameRequiredCode` | class | Parameter name is required. |
| `ParameterRequiredWithDefaultCode` | class | Required parameter cannot have a default value. |
| `ParameterTypeMismatchCode` | class | Parameter has incorrect type. |
| `ParameterTypeRequiredCode` | class | Parameter type is required. |
| `ParametersMissingCode` | class | Required parameters are missing. |
| `PathIdRequiredCode` | class | Path ID is required. |
| `PathMissingSpecificationCode` | class | Path must have either FullPath or Segments specified. |

## Installation

```bash
dotnet add package Fdw.Data.Builders --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Data.DataSets` · `Fdw.Data.DataSets.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
