# Fdw.CodeBuilder.CSharp

A C# code builder — emits syntax rather than concatenating strings.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `CodeBuilderCSharpResultCodes` | class | TypeCollection for CodeBuilder CSharp result codes. Codes use categorized numbers (Id == EventId ==… |

## Options (5 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ParseCancelledCode` | class | Parse operation was cancelled. |
| `ParseFailedCode` | class | Parse operation failed with exception. |
| `SourceCodeRequiredCode` | class | Source code was null or empty. |
| `SyntaxErrorsCode` | class | Source code contains syntax errors. |
| `ValidationFailedCode` | class | Validation failed. |

## Installation

```bash
dotnet add package Fdw.CodeBuilder.CSharp --prerelease
```

## Dependencies

`Fdw.CodeBuilder.Abstractions` · `Fdw.Collections` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
