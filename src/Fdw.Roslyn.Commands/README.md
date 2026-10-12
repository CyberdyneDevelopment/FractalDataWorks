# Fdw.Roslyn.Commands

The Roslyn command catalogue — every code-analysis and refactoring operation FDW exposes, each one a declared, downstream-extensible member rather than a hard-coded tool.

This package declares 1 service/provider type(s).

## Options (185)

| Type | Kind | Purpose |
|---|---|---|
| `AddBracesCommand` | class | Command to add braces to single-line statements. |
| `AddBracesTranslator` | class | Translator for AddBracesCommand. |
| `AddDocumentCommand` | class | Command to add a document to a project. |
| `AddDocumentTranslator` | class | Translator for AddDocumentCommand. |
| `AddProjectReferenceCommand` | class | Command to add a project reference. |
| `AddProjectReferenceTranslator` | class | Translator for AddProjectReferenceCommand. |
| `AddUsingsCommand` | class | Command to add missing using directives to a file. |
| `AddUsingsTranslator` | class | Translator for AddUsingsCommand. |
| `AnalyzeComplexityCommand` | class | Command to calculate cyclomatic complexity for methods. |
| `AnalyzeComplexityTranslator` | class | Translator for analyzing cyclomatic complexity. |
| `AnalyzeCouplingCommand` | class | Command to analyze coupling between types (afferent and efferent coupling). |
| `AnalyzeCouplingTranslator` | class | Translator for analyzing type coupling. |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `RoslynCommandHandler` | class | Default implementation of . Orchestrates command execution between workspace and translators. |

## Types (147)

| Type | Kind | Purpose |
|---|---|---|
| `AddDocumentResult` | class | Result of adding a document to a project. |
| `AddProjectReferenceResult` | class | Result of adding a project reference. |
| `AnalyzeFamilyDriftTranslatorLog` | class | MessageLogging methods for AnalyzeFamilyDriftTranslator. EventId range: 9300-9349. |
| `AssemblyUsage` | class | How much a document depends on one assembly. |
| `AssemblyUsageScanner` | class | Resolves every symbol a document references to the assembly that declares it. |
| `BaselineData` | class | Data returned from baseline information operations. |
| `BreakFinding` | class | Something that will break as a result of moving a type between assemblies. |
| `BuildProjectData` | class | Data returned by build project operation. |
| `CallHierarchyData` | class | Data returned by call hierarchy analysis. |
| `CallHierarchyEntry` | class | Represents a call hierarchy entry. |
| `ChangeLedger` | class | Default in-memory implementation of . Thread-safe: the stdio host may dispatch tool calls concurrently. |
| `ChangeLedgerLog` | class | MessageLogging methods for change ledger operations. |

## Installation

```bash
dotnet add package Fdw.Roslyn.Commands --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Roslyn.Commands.Abstractions` · `Fdw.Workspace.Roslyn`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
