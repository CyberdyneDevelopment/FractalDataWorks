# Fdw.MessageLogging.SourceGenerators

The MessageLogging generator: turns a partial `*Log` class into `LoggerMessage`-backed methods that log and return an `IGenericMessage` carrying the same EventId as its result code.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `LoggerMessageGenerator` | class | — |
| `LoggerMessageGenerator` | class | — |
| `LoggerMessageGenerator` | class | — |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.MessageLogging.SourceGenerators --prerelease
```

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
