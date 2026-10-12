# Fdw.Analyzers

The core FDW analyzers — result, logging and naming rules enforced at compile time.

Roslyn analyzers that enforce FDW conventions at compile time.

> **These rules do not ship.** This project is not packable and is not packed to `analyzers/dotnet/cs` by any package here, so the diagnostics below bind this repository only. Downstream consumers get the generated code, not the rules.

## Diagnostics

`FDW001` · `FDW002` · `FDW003` · `FDW004` · `FDW012` · `FDW013` · `FDW014` · `FDW015` · `FDW016` · `FDW022` · `FDW023`

## Analyzers

| Type | Kind | Purpose |
|---|---|---|
| `AsyncSuffixAnalyzer` | class | Analyzer that warns against using the 'Async' suffix on method names. Fdw convention: async methods… |
| `BrokenResultChainAnalyzer` | class | Analyzer that detects when a new GenericResult is created by extracting properties from an existing… |
| `DirectLoggerCallAnalyzer` | class | Analyzer that warns against direct ILogger calls outside of MessageLogging static classes. Fdw… |
| `ExceptionNotPropagatedAnalyzer` | class | Analyzer that detects catch blocks in methods returning IGenericResult where the caught exception's… |
| `ManualGenericMessageAnalyzer` | class | Analyzer that warns against manually creating GenericMessage instances in production code. Fdw… |
| `PlainStringFailureAnalyzer` | class | Analyzer that warns against using plain string messages in GenericResult.Failure(). Fdw convention: Use… |
| `SwallowedExceptionAnalyzer` | class | Analyzer that detects catch clauses where the caught exception is lost. Emits two related diagnostics:… |
| `UncheckedGenericResultAnalyzer` | class | Analyzer that warns when a GenericResult value is not checked for success or failure. Fdw convention:… |
| `UncheckedResultValueAccessAnalyzer` | class | Analyzer that warns when IGenericResult&lt;T&gt;.Value is accessed without first checking IsSuccess or… |
| `UnhandledFailurePathAnalyzer` | class | Analyzer that warns when a GenericResult is checked for success but the failure path is silently… |

## Installation

```bash
dotnet add package Fdw.Analyzers --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
