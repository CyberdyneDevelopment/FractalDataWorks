# Fdw.Data.OpenApi

OpenAPI document parsing into FDW containers and fields.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `OpenApiResultCodes` | class | TypeCollection for OpenAPI translator result codes. EventId range: 4350-4399 (within Data.OpenApi) |

## Options (6 declared)

| Type | Kind | Purpose |
|---|---|---|
| `NoMatchingOperationCode` | class | No matching OpenAPI operation found for the command. |
| `RequiredParameterMissingCode` | class | Required parameter missing from command. |
| `SchemaValidationFailedCode` | class | Schema validation failed. |
| `SpecParsingFailedCode` | class | OpenAPI specification parsing failed. |
| `TranslationExceptionCode` | class | Translation failed with an exception. |
| `TranslationSucceededCode` | class | Translation completed successfully. |

## Installation

```bash
dotnet add package Fdw.Data.OpenApi --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
