# Fdw.Messages

Structured messages: `GenericMessage`, the message severities, and the collection types that carry a failure's reason chain alongside a result.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `MessageSeverities` | class | TypeCollection for framework message severity levels. |

## Options (5 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CriticalMessageSeverity` | class | — |
| `DebugMessageSeverity` | class | — |
| `ErrorMessageSeverity` | class | — |
| `InformationMessageSeverity` | class | — |
| `WarningMessageSeverity` | class | — |

## Installation

```bash
dotnet add package Fdw.Messages --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
