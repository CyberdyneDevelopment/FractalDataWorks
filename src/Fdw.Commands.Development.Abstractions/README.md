# Fdw.Commands.Development.Abstractions

Development command contracts — the catalogue an MCP surface enumerates.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IDevelopmentCommand` | interface | Represents a command that operates on development artifacts (code, projects, solutions). This is the… |
| `IDevelopmentCommandCategory` | interface | Represents a category of development commands (Analysis, Compilation, Formatting, etc.). Categories are… |
| `IDevelopmentCommandResult` | interface | Represents the result of executing a development command. |
| `IDevelopmentCommandTranslator` | interface | Translates a development command into an operation on a context (Solution, AST, etc.). |
| `IDevelopmentCommandTranslator<in TCommand, in TContext, TResult>` | interface | Strongly-typed translator for a specific command type and context. |

## Base types (7)

| Type | Kind | Purpose |
|---|---|---|
| `DevelopmentCommandBase` | class | Base class for development commands. Commands are stateless data objects that describe an operation to… |
| `DevelopmentCommandCategories` | class | Type collection for development command categories. Categories are shared across all language… |
| `DevelopmentCommandCategoryBase` | class | Base class for development command categories. |
| `DevelopmentCommandTranslatorBase` | class | Base class for development command translators. |
| `DevelopmentCommandTranslatorBase<TCommand, TContext, TResult>` | class | Strongly-typed base class for development command translators. |
| `DevelopmentCommandTranslators` | class | Type collection for development command translators. |
| `DevelopmentCommands` | class | Type collection for all development commands. Language-specific commands (Roslyn, JavaScript, etc.) are… |

## Models and supporting types (11)

| Type | Kind | Purpose |
|---|---|---|
| `AnalysisCommandCategory` | class | Category for code analysis commands (complexity, dependencies, diagnostics, etc.). |
| `CodeSearchCommandCategory` | class | Category for code search commands (find usages, implementations, duplicates, etc.). |
| `CompilationCommandCategory` | class | Category for compilation commands (build, emit, diagnostics, syntax validation, etc.). |
| `DevelopmentCommandParameter` | class | Describes a parameter for a development command. |
| `DevelopmentCommandResult` | class | Default implementation of . |
| `FormattingCommandCategory` | class | Category for code formatting commands (format document, organize imports, etc.). |
| `GenerationCommandCategory` | class | Category for code generation commands (generate class, method, tests, etc.). |
| `NavigationCommandCategory` | class | Category for code navigation commands (find definition, base types, members, etc.). |
| `ProjectCommandCategory` | class | Category for project management commands (add/remove documents, references, etc.). |
| `RefactoringCommandCategory` | class | Category for refactoring commands (rename, extract method, encapsulate field, etc.). |
| `WorkspaceCommandCategory` | class | Category for workspace commands (snapshots, baseline, workspace info, etc.). |

## Installation

```bash
dotnet add package Fdw.Commands.Development.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
