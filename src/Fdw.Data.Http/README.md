# Fdw.Data.Http

HTTP data support: the translators that turn a data command into an HTTP request.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `DataHttpResultCodes` | class | TypeCollection for Data.Http result codes. EventId range: 5670-5689 (within Data domain) |

## Options (3 declared)

| Type | Kind | Purpose |
|---|---|---|
| `EndpointContainerType` | class | Container type for REST API endpoints. |
| `QueryTranslationFailedCode` | class | Failed to translate query to HTTP request. |
| `RequiredParameterMissingCode` | class | Required path parameter is missing. |

## Installation

```bash
dotnet add package Fdw.Data.Http --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
