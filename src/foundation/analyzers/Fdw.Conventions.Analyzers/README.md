# Fdw.Conventions.Analyzers

Convention analyzers — file layout, complexity and comparison rules.

Roslyn analyzers that enforce FDW conventions at compile time.

> **These rules do not ship.** This project is not packable and is not packed to `analyzers/dotnet/cs` by any package here, so the diagnostics below bind this repository only. Downstream consumers get the generated code, not the rules.

## Diagnostics

`FDW005` · `FDW006` · `FDW007` · `FDW008` · `FDW009` · `FDW010` · `FDW011` · `FDW017` · `FDW018` · `FDW019` · `FDW020` · `FDW021` · `FDW073`

## Analyzers

| Type | Kind | Purpose |
|---|---|---|
| `DuplicateTypeNameAnalyzer` | class | Analyzer that warns when multiple types in the same compilation share the same simple name (in different… |
| `FileNameMustMatchTypeNameAnalyzer` | class | Analyzer that enforces file names must match the type name declared within. Replaces MA0048 with support… |
| `MethodNameUnderscoreAnalyzer` | class | Analyzer that warns when method names contain underscores. Skips test methods, P/Invoke declarations,… |
| `MethodTooComplexAnalyzer` | class | Analyzer that warns when a method exceeds the configured maximum cyclomatic complexity. |
| `MethodTooLongAnalyzer` | class | Analyzer that warns when a method exceeds the configured maximum number of executable lines. Replaces… |
| `MisplacedImplementationTypeAnalyzer` | class | Analyzer that detects implementation-specific types in Abstractions or base service assemblies. Types… |
| `NoCodedLimitAnalyzer` | class | FDW073 (FDW-7059): a public int or long constant named Max, Maximum, Default or Limit is a limit written in code and an error. A limit is configuration: declare its default in the service type option's `DeclaredDefaults`, read it at Register with `DeclaredSetting.ConfiguredCount` (a host changes it with `ServiceTypeDefaults.Override`, a value the host's configuration states wins), then hand the configured value to what checks it (see `DefaultMessagingServiceType`). A number a protocol or file format fixes is marked `[FormatLimit("reason")]`; a cap found before the rule carries `[ConfigurationPending("FDW-n")]` naming the story that removes it. |
| `TypeCollectionOpportunityAnalyzer` | class | Analyzer that identifies enum declarations and enum-based dispatch patterns that should be replaced with… |
| `UnimplementedAbstractTypeAnalyzer` | class | Analyzer that reports interfaces and abstract classes defined in source that have no concrete… |
| `UnusedTypeAnalyzer` | class | Analyzer that reports source-defined types that are never referenced anywhere in the current… |

## Installation

```bash
dotnet add package Fdw.Conventions.Analyzers --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; edit by hand, nothing regenerates it -->
