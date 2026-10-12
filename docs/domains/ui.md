# Domain: UI

## Purpose

The UI domain supplies framework-neutral UI building blocks: page and form models, navigation, themes, and a terminal renderer. It ships no web UI. The web console is the separate `reference-react-ui` repository, which calls the API over HTTP.

The Blazor packages (`Fdw.UI.Components.Blazor`, the MudBlazor and Tailwind skins, the per-domain `*.Components` and `*.UI.Pages` packages, and the Blazor authentication packages) were removed in FDW-872.

## Projects

| Project | What it holds |
|---------|---------------|
| `Fdw.UI.Abstractions` | CRTP abstractions for type-safe UI components; no UI technology dependency |
| `Fdw.UI.Components` | Page models (`ListPageModel`, `DetailPageModel`, `WizardPageModel`, ...), form field models, icons, status variants |
| `Fdw.UI` | Navigation (`NavSections`, `NavTree`, `PageTypes`) and component-type options |
| `Fdw.UI.Web.Abstractions` | Render-mode and web-component metadata types |
| `Fdw.UI.Services` | Client-side services: commands, formula, pipeline, undo/redo |
| `Fdw.UI.Themes` (+ `.Abstractions`, `.Clients`, `.Endpoints`, `.Registration`) | Theme TypeCollections: `ColorPalettes`, `BorderStyles`, `IconSets`, `MenuThemes` |
| `Fdw.UI.Rendering.Spectre` | Spectre.Console renderer for the models |
| `Fdw.UI.Components.TUI` | Terminal implementation of the UI components |
| `Fdw.UI.Components.RazorConsole` | Razor syntax that compiles to Spectre.Console output (terminal only) |
| `Fdw.TUI.Management` | The terminal management UI |
| `Fdw.UI.Pipelines.Clients` / `.Endpoints` | Pipeline designer API contract and endpoint bases |

## Patterns

### Models, not components

A screen is described by models from `Fdw.UI.Components`. A renderer draws them. The models carry state and field metadata and hold no rendering code, so the same model can be drawn by any renderer. Today `Fdw.UI.Rendering.Spectre` is the renderer.

### Data comes from `*.Clients`

The terminal UI reads and writes through the per-domain `*.Clients` packages. It never uses `IDataGateway`. The bearer token is attached by `Web.Http.Authentication`'s `BearerTokenHandler`.

### Theme engine

Tenant themes use HSL tokens that map to CSS variables (`ThemeHelper` in `Fdw.Services.Multitenancy.Abstractions`):

```csharp
theme.ToCssRootBlock()      // --fdw-primary-h, --fdw-primary-s, etc.
theme.ToScalarCssBlock()    // --scalar-color-1, etc. (Scalar API docs)
```

## Rules

1. **Models hold no rendering code.** A model never references a renderer package.
2. **UI code never touches data directly.** It calls a `*.Clients` package, which calls the API.
3. **API clients are unsealed with virtual methods** so tests can mock them with Moq.
4. **Theme tokens are HSL-based.** Never use hex or RGB values directly.

## Related Domains

- **Services**: the `*.Clients` packages the UI calls
- **Web**: `Web.Clients.Abstractions` defines the shared DTO contracts
- **Multitenancy**: tenant themes (`ITenantTheme`, `ThemeHelper`)
