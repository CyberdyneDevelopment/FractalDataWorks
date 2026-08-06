# Fdw.Collections.SourceGenerators

The generators behind TypeCollections: they emit each collection's members, its typed lookups, and the module initializer that collects and dedupes options across every referenced assembly.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `MutableServiceTypeCollectionGenerator` | class | Generator for mutable ServiceTypeCollections using ConcurrentDictionary with Register() method. |
| `MutableTypeCollectionGenerator` | class | Generator for mutable TypeCollections using ConcurrentDictionary with Register() method. |
| `ServiceTypeCollectionGenerator` | class | Generator for immutable ServiceTypeCollections using FrozenDictionary. |
| `ServiceTypeInstanceCollectionGenerator` | class | Generator for factory-based ServiceTypeCollections that create new instances. |
| `TypeCollectionGenerator` | class | Generator for immutable TypeCollections using FrozenDictionary. |
| `TypeInstanceCollectionGenerator` | class | Generator for factory-based TypeCollections that create new instances. |
| `TypeOptionExtensionGenerator` | class | Generates C# 14 static extension methods for TypeOption and ServiceTypeOption classes. Each TypeOption… |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.Collections.SourceGenerators --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
