# Fdw.Configuration.UI.SourceGenerators

Generates the form metadata that lets a UI render a configuration type it has never seen.

This package is a Roslyn incremental source generator. It is referenced with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`, so it contributes generated code at compile time and ships no runtime assembly of its own.

## Generators

| Type | Kind | Purpose |
|---|---|---|
| `BlazorComponentGenerator` | class | Generates Blazor-specific components with RenderTreeBuilder. |
| `ConfigurationFormModelGenerator` | class | Generates ConfigurationFormModel derived classes for [ManagedConfiguration] classes. Creates… |
| `ConfigurationUISourceGenerator` | class | Incremental source generator that creates Web, Blazor, and TUI components for classes annotated with… |
| `TUIComponentGenerator` | class | Generates Terminal UI components with Spectre.Console. |
| `WebComponentGenerator` | class | Generates framework-agnostic web components with metadata. |

Generated sources are emitted per compilation. To read what a generator produced, build with `EmitCompilerGeneratedFiles` and look under `obj/generated/`.

## Installation

```bash
dotnet add package Fdw.Configuration.UI.SourceGenerators --prerelease
```

## Dependencies

`Fdw.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
