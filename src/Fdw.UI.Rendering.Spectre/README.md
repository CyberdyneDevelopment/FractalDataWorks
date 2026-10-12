# Fdw.UI.Rendering.Spectre

The Spectre.Console renderer — the same page model, drawn in a terminal.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SpectreUIResultCodes` | class | TypeCollection for Spectre UI result codes. Codes use the categorized-number scheme (Id == EventId ==… |
| `WizardActions` | class | TypeCollection for wizard navigation actions. |

## Options (8 declared)

| Type | Kind | Purpose |
|---|---|---|
| `InvalidRenderContextCode` | class | Invalid render context, expected SpectreRenderContext. |
| `CancelWizardAction` | class | — |
| `CompleteWizardAction` | class | — |
| `EditFieldsWizardAction` | class | — |
| `NextWizardAction` | class | — |
| `NoneWizardAction` | class | — |
| `PreviousWizardAction` | class | — |
| `SkipWizardAction` | class | — |

## Installation

```bash
dotnet add package Fdw.UI.Rendering.Spectre --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.UI.Abstractions` · `Fdw.UI.Components` · `Fdw.UI.Themes`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
