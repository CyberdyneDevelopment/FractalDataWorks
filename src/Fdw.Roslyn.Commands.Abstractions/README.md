# Fdw.Roslyn.Commands.Abstractions

The Roslyn command contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (19)

| Type | Kind | Purpose |
|---|---|---|
| `IBaselineAwareCommand` | interface | Marks a command that needs the workspace's baseline solution injected before translation. |
| `IBaselineSettingCommand` | interface | Marks a command that advances the workspace baseline to the current solution. |
| `IChangeLedger` | interface | Records mutating command effects for the current session and can render them as a migration-guide… |
| `IFileChangeType` | interface | Interface for file change types. |
| `ILedgerAwareCommand` | interface | Marks a command as needing the session's injected by the handler before translation, mirroring the… |
| `ILedgerClearingCommand` | interface | Marks the one command permitted to discard the change ledger. |
| `IMismatchKind` | interface | Marker interface for the kind of disagreement between a type's namespace, its file path and its owning… |
| `IReasonedCommand` | interface | Marks a command that carries the caller's reason for making the change. |
| `IRoslynCommand` | interface | Represents a command that operates on a Roslyn solution. |
| `IRoslynCommandCategory` | interface | Represents a category of Roslyn commands. Extends for C# specific categorization. |
| `IRoslynCommandHandler` | interface | Handles execution of Roslyn commands, orchestrating between workspace and translators. |
| `IRoslynCommandResult` | interface | Marker interface for Roslyn command results. |
| `IRoslynCommandTranslator` | interface | Translates a Roslyn command into an operation on a Solution. Extends for Roslyn-specific translation. |
| `IRoslynCommandTranslator<in TCommand, TResult>` | interface | Strongly-typed translator for a specific Roslyn command type. |
| `ISnapshotCreatingCommand` | interface | Marks a command that creates a snapshot, which only the workspace can actually store. |
| `ISnapshotRestoringCommand` | interface | Marks a command that restores a stored snapshot and needs it resolved before translation. |

## Base types (16)

| Type | Kind | Purpose |
|---|---|---|
| `FileChangeTypeBase` | class | Base class for file change types. |
| `FileChangeTypes` | class | TypeCollection for file change types. |
| `MismatchKindBase` | class | Base class for options. |
| `MismatchKinds` | class | The ways a type's namespace can disagree with where it physically lives. |
| `RoslynCommandBase` | class | Base class for Roslyn commands. Commands are stateless data objects that describe an operation to… |
| `RoslynCommandCategories` | class | Type collection for Roslyn command categories. |
| `RoslynCommandCategoryBase` | class | Base class for Roslyn command categories. Extends for C# specific categorization. |
| `RoslynCommandTranslatorBase` | class | Base class for Roslyn command translators. Extends for Roslyn-specific translation. |
| `RoslynCommandTranslatorBase<TCommand, TResult>` | class | Strongly-typed base class for Roslyn command translators. |
| `RoslynCommandTranslatorBase<TSelf, TCommand, TResult>` | class | A translator that receives a logger typed to itself. |

## Models and supporting types (111)

| Type | Kind | Purpose |
|---|---|---|
| `AnalysisCommandCategory` | class | Command category for code analysis operations. |
| `BothMismatchKind` | class | The namespace disagrees with both the file path and the owning project. |
| `ChangeCannotBeVerifiedCode` | class | The change could not be verified because an affected project's compilation cannot bind. |
| `ChangeLedgerData` | class | Query data returned by GetChangeLedger, summarizing the session's recorded changes. |
| `ChangeLedgerEntry` | class | A single recorded entry in the change ledger, capturing one mutating command's effects. |
| `ChangeWouldNotCompileCode` | class | The rewrite would leave affected projects unable to compile. |
| `ClassNameRequiredCode` | class | Class name is required. |
| `CommandCannotBeNullCode` | class | Command cannot be null. |
| `CommandExecutionCancelledCode` | class | Command execution was cancelled. |
| `CommandExecutionFailedCode` | class | Command execution failed. |
| `CommandTypeCannotBeNullCode` | class | Command type cannot be null. |
| `CompilationCommandCategory` | class | Command category for compilation operations. |

## Installation

```bash
dotnet add package Fdw.Roslyn.Commands.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Development.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
