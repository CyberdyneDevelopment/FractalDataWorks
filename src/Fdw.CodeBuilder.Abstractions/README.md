# Fdw.CodeBuilder.Abstractions

Contracts for programmatic code construction.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (14)

| Type | Kind | Purpose |
|---|---|---|
| `IClassBuilder` | interface | Builder interface for generating class definitions. |
| `ICodeBuilder` | interface | Base interface for code builders that generate source code. |
| `ICodeGenerator` | interface | Interface for code generators that transform syntax trees or definitions into source code. |
| `ICodeParser` | interface | Defines a parser that can convert source code into a syntax tree. |
| `IConstructorBuilder` | interface | Builder interface for generating constructor definitions. |
| `IEnumBuilder` | interface | Builder interface for generating enum definitions. |
| `IFieldBuilder` | interface | Builder interface for generating field definitions. |
| `IInputInfoModel` | interface | Interface for models that support input change tracking for incremental generation. Used by code… |
| `IInterfaceBuilder` | interface | Builder interface for generating interface definitions. |
| `ILanguageRegistry` | interface | Registry for managing language-specific parsers. |
| `IMethodBuilder` | interface | Builder interface for generating method definitions. |
| `IPropertyBuilder` | interface | Builder interface for generating property definitions. |
| `ISyntaxNode` | interface | Represents a node in the syntax tree. |
| `ISyntaxTree` | interface | Represents a parsed syntax tree. |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `InputHashCalculator` | class | Provides hash calculation functionality for IInputInfoModel implementations. |

## Installation

```bash
dotnet add package Fdw.CodeBuilder.Abstractions --prerelease
```

## Dependencies

`Fdw.Results`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
