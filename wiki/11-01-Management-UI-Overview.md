# Management UI Overview

FDW has two management UIs. Both talk to the same REST API; neither contains domain logic.

| UI | Where it lives | Technology | How it reaches the API |
|----|----------------|------------|------------------------|
| Web console | `reference-react-ui` repository | React + TypeScript (Vite) | Its own HTTP calls, from one endpoint table (`src/core/endpoints.ts`) |
| Terminal UI | `Fdw.TUI.Management` in this repo | Spectre.Console | The typed `*.Clients` packages |

FDW itself ships no web UI. The Blazor skins (`reference-ui`, `reference-aui`) and the FDW Blazor packages they used (`*.Components`, `*.UI.Pages`, `Fdw.UI.Components.Blazor`, the Blazor authentication packages) were removed in FDW-872.

## Architecture

```
┌──────────────────────────┐        ┌───────────────────────────────────┐
│  reference-react-ui      │        │  Fdw.TUI.Management               │
│  (web console)           │        │  Fdw.UI.Components models         │
│                          │        │  drawn by Fdw.UI.Rendering.Spectre│
└────────────┬─────────────┘        └─────────────────┬─────────────────┘
             │                                        │
             │                           ┌────────────▼────────────┐
             │                           │  {Domain}.Clients       │
             │                           └────────────┬────────────┘
             │  HTTP + bearer token                   │  HTTP + bearer token
             ▼                                        ▼
┌───────────────────────────────────────────────────────────────────────┐
│  REST API (reference-api): FDW *.Endpoints closed by the host         │
└───────────────────────────────────────────────────────────────────────┘
```

## Web console (`reference-react-ui`)

- Every route the console calls is declared once in `src/core/endpoints.ts`, with an `expected` state of `live`, `proposed` or `absent`. The same table drives the fetcher's URL, the marker a panel shows when a call does not answer, and the console's gap report.
- The console is theme-independent: shape, type and spacing come from tokens, never from theme names.
- Run and deploy instructions are in that repository's README.

## Terminal UI (`Fdw.TUI.Management`)

- Screens are built from the framework-neutral page and form models in `Fdw.UI.Components` (`ListPageModel`, `DetailPageModel`, `WizardPageModel`, `TextInputModel`, and so on).
- `Fdw.UI.Rendering.Spectre` draws those models in the terminal.
- Data comes from the per-domain `*.Clients` packages, which attach the bearer token through `Web.Http.Authentication`'s `BearerTokenHandler`.

## Adding a screen

The UI owns no domain logic, so a new screen needs no new FDW service code.

1. Confirm the API route exists: a concrete endpoint class **and** its `[TypeOption]` declaration in the host's endpoint collection. Endpoint auto-discovery is off, so a class with no option returns 404.
2. **Web console:** add the route to `src/core/endpoints.ts`, then build the screen from the console's shared components.
3. **Terminal UI:** call the domain's `*.Clients` method and build the screen from `Fdw.UI.Components` models.

## See Also

- [13-05 UI Layer Anatomy](13-05-UI-Layer-Anatomy.md): object types at each layer
- [16-01 API Clients Reference](16-01-API-Clients-Reference.md): all `*.Clients` packages
- [12-11 JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md): client-side token handling
