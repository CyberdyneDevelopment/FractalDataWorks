# Scalar Theming Bridge

`ThemeHelper` in `Fdw.Services.Multitenancy.Abstractions` maps FDW tenant theme tokens (`ITenantTheme`) to Scalar API Reference CSS variables (`--scalar-*`). Inject the result via `AddHeadContent()` on `MapScalarApiReference`.

**Package:** `Fdw.Services.Multitenancy.Abstractions`
**Dependency:** `ColorHelper` NuGet package (HSL-to-hex conversion)

## Overview

FDW themes store colors as HSL strings (`"221 83% 53%"`). Scalar uses `--scalar-*` CSS custom properties on `.dark-mode` / `.light-mode` body classes. `ThemeHelper` bridges the two.

```csharp
// Existing FDW CSS bridge (Blazor UI)
string fdwCss = theme.ToCssRootBlock();       // → :root { --color-primary: ... }

// New Scalar bridge (API docs)
string scalarCss = theme.ToScalarCssBlock();  // → .dark-mode { --scalar-color-1: ... }
```

## Methods

### `ToScalarCssVariables()`

Returns a dictionary of Scalar CSS variable names to hex color values:

```csharp
IDictionary<string, string> vars = theme.ToScalarCssVariables();
// {
//   "--scalar-color-1": "#1a1a2e",
//   "--scalar-background-1": "#0f3460",
//   "--scalar-button-1": "#e94560",
//   ...
// }
```

**Mapping:**

| Scalar Variable | FDW Token |
|----------------|-----------|
| `--scalar-color-1` | `TextMainColor` |
| `--scalar-color-2` | `TextMutedColor` |
| `--scalar-color-3` | `TextMutedColor` |
| `--scalar-color-accent` | `AccentColor` |
| `--scalar-background-1` | `BackgroundColor` |
| `--scalar-background-2` | `SurfaceColor` |
| `--scalar-background-3` | `OverlayColor` |
| `--scalar-background-accent` | `AccentColor` + `1f` (alpha) |
| `--scalar-color-green` | `SuccessColor` |
| `--scalar-color-red` | `ErrorColor` |
| `--scalar-color-yellow` | `WarningColor` |
| `--scalar-color-blue` | `InfoColor` |
| `--scalar-border-color` | `OverlayColor` |
| `--scalar-button-1` | `PrimaryColor` |
| `--scalar-button-1-color` | `TextMainColor` |
| `--scalar-sidebar-background-1` | `SurfaceColor` |
| `--scalar-sidebar-color-1` | `TextMainColor` |
| `--scalar-sidebar-color-2` | `TextMutedColor` |
| `--scalar-sidebar-border-color` | `OverlayColor` |

### `ToScalarCssBlock(bool? darkMode = null)`

Generates a ready-to-inject CSS block:

```csharp
// Target .dark-mode only (default)
string css = theme.ToScalarCssBlock(darkMode: true);
// → .dark-mode { --scalar-color-1: #...; ... }

// Target .light-mode only
string css = theme.ToScalarCssBlock(darkMode: false);
// → .light-mode { --scalar-color-1: #...; ... }

// Both selectors with the same values
string css = theme.ToScalarCssBlock(darkMode: null);
// → .dark-mode { ... } .light-mode { ... }
```

## Usage with MapScalarApiReference

```csharp
// Program.cs (API server)
var tenant = app.Services.GetRequiredService<ITenantThemeProvider>();
var theme = await tenant.GetCurrentTheme();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API")
        .WithTheme(ScalarTheme.None)   // Disable Scalar's built-in theme
        .EnableDarkMode()
        .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
        .AddHeadContent(@$"<style>{theme.ToScalarCssBlock()}</style>");
});
```

If the theme is loaded per-tenant at request time rather than at startup, inject the CSS block via a middleware or dynamic head injection instead.

## Static CSS Block

For a fixed theme (no multitenancy), build the CSS block once at startup:

```csharp
// appsettings-driven theme
var theme = builder.Configuration.GetSection("Theme").Get<TenantTheme>();

string scalarCss = theme is not null
    ? theme.ToScalarCssBlock()
    : string.Empty;

app.MapScalarApiReference(options =>
{
    options
        .WithTheme(ScalarTheme.None)
        .EnableDarkMode()
        .AddHeadContent($"<style>{scalarCss}</style>");
});
```

## FDW HSL Format

FDW themes store HSL as `"h s% l%"` strings (e.g. `"221 83% 53%"`). `ThemeHelper.HslToHex()` parses this format internally and converts using the `ColorHelper` package:

```csharp
// Internal conversion (used by ToScalarCssVariables)
string hex = ThemeHelper.HslToHex("221 83% 53%");
// → "#1e6de8"
```

Hex values are output as lowercase 6-digit strings (e.g., `#1e6de8`). The `--scalar-background-accent` variable appends `1f` (12% alpha in RGBA hex notation).

## Related

- Management UI theming docs live in the separate **reference-ui** repository.
- `ThemeHelper.ToCssRootBlock()` — FDW Blazor UI CSS bridge
- [Creating a Server](12-01-Creating-A-Server.md)
