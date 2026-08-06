# Fdw.Configuration.SourceGenerators

The configuration generators: DDL, validators and mappers emitted from a `[ManagedConfiguration]` class.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationSourceGenerator` | class | Incremental source generator that creates DDL definitions, ConfigurationTypes collection, and… |
| `ConfigurationTypesGenerator` | class | Utility helpers for configuration source generation. |
| `DdlGenerator` | class | Generates DDL definition code for configuration classes. |
| `TypeCollectionDdlGenerator` | class | Generates DDL definitions for TypeCollections referenced by [ConfigurationOption]. |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.Configuration.SourceGenerators --prerelease
```

## Dependencies

`Fdw.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
