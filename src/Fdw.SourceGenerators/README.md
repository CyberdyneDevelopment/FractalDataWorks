# Fdw.SourceGenerators

Shared generator infrastructure used by the other FDW generators.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `EmptyClassGenerator` | class | Generates Empty classes for collection value types. |
| `FieldGenerator<TId>` | class | Generates field declarations for collection classes. Responsible for creating _all, _empty, and lookup… |
| `LookupMethodGenerator` | class | Generates lookup methods for collection classes (Name, Id, etc.). Uses conditional compilation for NET8+… |
| `StaticConstructorGenerator<TId>` | class | Generates static constructors for collection classes. |
| `ValuePropertyGenerator<TId>` | class | Generates static properties or methods for collection values. |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.SourceGenerators --prerelease
```

## Dependencies

`Fdw.CodeBuilder.Abstractions` · `Fdw.CodeBuilder.CSharp`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
