# Fdw.Conventions.CodeFixes

Code fixes for the convention analyzers.

Roslyn code-fix providers for the matching analyzer package. Not packable — these fixes are available in this repository's IDE and build, not to consumers.

## Fixes

| Type | Kind | Purpose |
|---|---|---|
| `AddConfigureAwaitCodeFixProvider` | class | Code fix provider that adds ConfigureAwait(false) to awaited expressions. Fixes AsyncFixer04. |
| `AddStringComparisonCodeFixProvider` | class | Code fix provider that adds StringComparison argument to string methods. Fixes MA0006 (Use string.Equals… |
| `MoveTypeToFileCodeFixProvider` | class | Code fix provider that moves a type declaration to its own file. Fixes FDW005 (file name must match type… |
| `RemoveMethodNameUnderscoreCodeFixProvider` | class | Code fix provider that removes underscores from method names and updates all references. |
| `SuppressWithConventionOverrideCodeFixProvider` | class | Code fix provider that adds [ConventionOverride] attribute to suppress FDW006 or FDW007. |

## Installation

```bash
dotnet add package Fdw.Conventions.CodeFixes --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
