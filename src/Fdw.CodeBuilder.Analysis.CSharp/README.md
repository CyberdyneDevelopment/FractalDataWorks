# Fdw.CodeBuilder.Analysis.CSharp

C# analysis over built code.

This package declares 1 interface(s), 4 service/provider type(s), 4 model(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `I` | interface | — |

## Services (4)

| Type | Kind | Purpose |
|---|---|---|
| `AssemblyCompilationBuilder` | class | Builder for creating Roslyn compilations with assembly-level attributes and metadata. Supports… |
| `ExpectationsFactory` | class | Factory class for creating syntax tree expectations. |
| `GeneratorPipelineBuilder` | class | Fluent API for building complex generator test scenarios. Combines functionality of other test utilities… |
| `TestSourceProvider` | class | Helper class for creating test sources. |

## Records (4)

| Type | Kind | Purpose |
|---|---|---|
| `ComplexInputInfoModel` | class | Another test implementation of with different properties. |
| `ComplexInputInfoModel` | class | Another test implementation of with different properties. |
| `TestInputInfoModel` | class | Mock implementation of . |
| `TestInputInfoModel` | class | Mock implementation of . |

## Types (46)

| Type | Kind | Purpose |
|---|---|---|
| `ClassExpectations` | class | Provides expectations for a class declaration. |
| `CodeBlockExpectations` | class | Provides expectations for code blocks. |
| `CompilationVerifier` | class | Helper class for verifying that generated code compiles and runs correctly. |
| `ConstructorExpectations` | class | Provides fluent assertions for validating constructor syntax and structure. |
| `DiagnosticGenerator` | class | Test source generator that reports diagnostics. |
| `EnumExpectations` | class | Provides fluent assertions for validating enum syntax and structure. |
| `EnumValueExpectations` | class | Provides fluent assertions for individual enum values. |
| `EqualsGenerator` | class | Generator that creates Equals/GetHashCode implementations for marked classes. |
| `ErrorSourceGenerator` | class | A mock implementation of a source generator that reports system errors. |
| `ErrorSourceGenerator` | class | A mock implementation of a source generator that reports system errors. |
| `ExpectationException` | class | Exception thrown when expectations are not met in test assertions. |
| `ExpectationException` | class | Exception thrown when an expectation validation fails. |

## Installation

```bash
dotnet add package Fdw.CodeBuilder.Analysis.CSharp --prerelease
```

## Dependencies

`Fdw.CodeBuilder.Abstractions` · `Fdw.CodeBuilder.Analysis`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
