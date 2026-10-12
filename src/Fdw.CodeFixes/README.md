# Fdw.CodeFixes

Code fixes for the core FDW analyzers.

Roslyn code-fix providers for the matching analyzer package. Not packable — these fixes are available in this repository's IDE and build, not to consumers.

## Fixes

| Type | Kind | Purpose |
|---|---|---|
| `AsyncSuffixCodeFixProvider` | class | Code fix provider that removes the 'Async' suffix from method names and updates all references. |
| `BrokenResultChainCodeFixProvider` | class | Code fix provider that replaces broken result chain patterns with ToNewResult() or Chain(). |
| `ExceptionNotPropagatedCodeFixProvider` | class | Code fix provider that wraps catch block logging calls in GenericResult.Failure() returns. |
| `UncheckedGenericResultCodeFixProvider` | class | Code fix provider that adds failure checking for unchecked GenericResult values. |
| `UncheckedResultValueAccessCodeFixProvider` | class | Code fix provider that wraps unguarded IGenericResult&lt;T&gt;.Value access in an IsSuccess check.… |
| `UnhandledFailurePathCodeFixProvider` | class | Code fix provider that adds an else clause for unhandled GenericResult failure paths. |

## Installation

```bash
dotnet add package Fdw.CodeFixes --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
