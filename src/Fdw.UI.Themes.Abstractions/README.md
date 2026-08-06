# Fdw.UI.Themes.Abstractions

Theme contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IBlazorTheme` | interface | Defines a Blazor CSS theme providing component and layout class tokens. Implementations map semantic… |
| `IBorderStyle` | interface | Defines border styles for UI components. |
| `IColorPalette` | interface | Defines a color palette for UI components. |
| `IIconSet` | interface | Defines icons and indicators for UI components. Icons are string-based to support Unicode, ASCII, or… |
| `IMenuTheme` | interface | Defines a complete theme combining color palette, border styles, and icons. Theme components are… |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `BlazorThemeBase` | class | Abstract base class for Blazor CSS themes. Subclasses populate in their constructor; provides null-safe… |
| `BorderStyleBase` | class | Abstract base class for border styles. Inherit from this class and apply [TypeOption] attribute to… |
| `ColorPaletteBase` | class | Abstract base class for color palettes. Inherit from this class and apply [TypeOption] attribute to… |
| `IconSetBase` | class | Abstract base class for icon sets. Inherit from this class and apply [TypeOption] attribute to create… |
| `MenuThemeBase` | class | Abstract base class for menu themes. Inherit from this class and apply [TypeOption] attribute to create… |

## Models and supporting types (4)

| Type | Kind | Purpose |
|---|---|---|
| `CreateThemeRequest` | class | Represents a request to create a new theme. |
| `ThemeConfiguration` | class | Represents a complete theme configuration with colors, typography, and branding settings. |
| `ThemeSummaryPayload` | class | Represents a summary of a theme for listing purposes. |
| `UpdateThemeRequest` | class | Represents a request to update an existing theme. |

## Installation

```bash
dotnet add package Fdw.UI.Themes.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
