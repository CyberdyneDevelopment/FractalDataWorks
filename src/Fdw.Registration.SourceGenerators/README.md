# Fdw.Registration.SourceGenerators

Emits the module initializer that enlists an assembly's service types at load — the mechanism that makes a package reference a registration.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationTypeModuleInitializerGenerator` | class | Generates module initializers in ENTRY POINT assemblies to register ConfigurationTypes from REFERENCED… |
| `PocoMapperModuleInitializerGenerator` | class | Generates module initializers in CONSUMING assemblies to register POCO mappers from REFERENCED… |
| `ServiceTypeOptionModuleInitializerGenerator` | class | Generates module initializers to register [ServiceTypeOption] types cross-assembly. |
| `TypeOptionModuleInitializerGenerator` | class | Generates module initializers in CONSUMING executable assemblies to register [TypeOption] types from… |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.Registration.SourceGenerators --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
