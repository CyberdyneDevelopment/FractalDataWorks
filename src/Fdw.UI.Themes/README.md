# Fdw.UI.Themes

Themes as configuration: a theme is data a renderer reads, not a stylesheet compiled in.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `BorderStyles` | class | TypeCollection of available border styles. |
| `ColorPalettes` | class | TypeCollection of available color palettes. |
| `IconSets` | class | TypeCollection of available icon sets. |
| `MenuThemes` | class | TypeCollection of available menu themes. |

## Options (12 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AsciiBorderStyle` | class | ASCII border style - maximum compatibility, uses only ASCII characters. |
| `AsciiIconSet` | class | ASCII icon set - maximum compatibility using only ASCII characters. |
| `DarkColorPalette` | class | Dark color palette - default theme for terminal UIs. |
| `DarkMenuTheme` | class | Dark menu theme - default theme for terminal UIs. |
| `HighContrastColorPalette` | class | High contrast color palette for accessibility. |
| `HighContrastMenuTheme` | class | High contrast menu theme for accessibility. |
| `LightColorPalette` | class | Light color palette for terminal UIs with light backgrounds. |
| `LightMenuTheme` | class | Light menu theme for terminal UIs with light backgrounds. |
| `RoundedBorderStyle` | class | Rounded border style - modern, soft appearance. |
| `SquareBorderStyle` | class | Square border style - classic, sharp corners. |
| `UnicodeIconSet` | class | Unicode icon set - uses Unicode symbols for modern terminals. |
| `ThemeConfigurationCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.UI.Themes --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Services` · `Fdw.Types.Abstractions` · `Fdw.UI.Themes.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
