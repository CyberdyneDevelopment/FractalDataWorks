# Fdw.Data.OData

OData query translation and metadata mapping.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ODataCommandTranslators` | class | TypeCollection of REST/OData data command translators. Discovered at compile-time via TypeCollection… |
| `ODataResultCodes` | class | TypeCollection for REST data translator result codes. Codes use categorized numbers (Id == EventId ==… |

## Options (15 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ContainerNullCode` | class | Container parameter is null. |
| `DeleteFilterInvalidCode` | class | DeleteCommand has invalid Filter expression. |
| `DeleteFilterRequiredCode` | class | DeleteCommand requires Filter in metadata for safety. |
| `DeleteResourceIdNotFoundCode` | class | Cannot determine resource ID from Filter expression. |
| `DeleteTranslationFailedCode` | class | Failed to translate DELETE command. |
| `InsertDataRequiredCode` | class | InsertCommand requires Data in metadata. |
| `InsertTranslationFailedCode` | class | Failed to translate INSERT command. |
| `ODataCommandTranslators` | class | TypeCollection of REST/OData data command translators. Discovered at compile-time via TypeCollection… |
| `ODataDeleteTranslator` | class | Translates DeleteCommand to REST DELETE request. |
| `ODataInsertTranslator` | class | Translates InsertCommand to REST POST request with JSON body. |
| `ODataQueryTranslator` | class | Translates QueryCommand to REST GET request with OData query parameters. |
| `ODataUpdateTranslator` | class | Translates UpdateCommand to REST PUT/PATCH request with JSON body. |
| `QueryTranslationFailedCode` | class | Failed to translate QUERY command. |
| `UpdateDataRequiredCode` | class | UpdateCommand requires Data in metadata. |
| `UpdateResourceIdNotFoundCode` | class | Cannot determine resource ID for update. |
| `UpdateTranslationFailedCode` | class | Failed to translate UPDATE command. |

## Installation

```bash
dotnet add package Fdw.Data.OData --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Connections.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
