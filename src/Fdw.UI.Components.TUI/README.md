# Fdw.UI.Components.TUI

Terminal UI components.

This package declares 1 configuration type(s).

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `TUIThemeConfiguration` | class | Theme configuration for Terminal UI components. |

## Types (10)

| Type | Kind | Purpose |
|---|---|---|
| `CollectionPromptHelper` | class | Helper for interactive collection management. |
| `ConfirmationPromptHelper` | class | Helper for creating Yes/No confirmation prompts. |
| `NumericPromptHelper` | class | Helper for creating numeric prompts with range validation. |
| `PanelRenderer` | class | Renders content in bordered panels. |
| `TUIComponent<TSelf, TModel>` | class | CRTP base for Terminal UI components using Spectre.Console. Provides interactive prompting and rich… |
| `TUIPropertyComponent<TSelf, TProperty>` | class | CRTP base for Terminal UI property-level components. |
| `TableRenderer` | class | Renders collections as formatted tables. |
| `TextPromptHelper` | class | Helper for creating text prompts with validation. |
| `TreeRenderer` | class | Renders hierarchical data as trees. |
| `TypeCollectionPromptHelper` | class | Helper for prompting TypeCollection selections. |

## Installation

```bash
dotnet add package Fdw.UI.Components.TUI --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.UI.Abstractions` · `Fdw.UI.Themes`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
