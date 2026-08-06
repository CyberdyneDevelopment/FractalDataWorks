# Management UI Overview

FractalDataWorks ships two reference Management UI implementations, each demonstrating a different Blazor hosting model on top of the shared FDW UI packages. Both connect to the same REST API backend and surface the same domain pages.

For a deep dive into the hosting models, see [Blazor Hosting Models](11-02-Blazor-Hosting-Models.md).

## UI Implementations

The reference UI projects live in their own repositories — they are not part of this repo.

| Repo | Hosting Model | Component Library | Styling |
|------|---------------|-------------------|---------|
| `reference-ui` | InteractiveServer | Custom (Tailwind) | Tailwind CSS v4 |
| `reference-aui` | InteractiveAuto | MudBlazor | MudBlazor + custom theme |

Both UIs:
- Reference FDW packages (`Fdw.UI.*`, per-domain `*.UI.Pages`, per-domain `*.Clients`)
- Use the headless-UI pattern (logic in framework providers, rendering in the skin)
- Discover pages via the `PageTypes` TypeCollection (see [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md))
- Talk to the same REST API backend over typed `*.Clients` packages

## Architecture

```
┌───────────────────────────────────────────────────────────────────────────┐
│                              UI Layer                                      │
│   ┌──────────────────────────┐  ┌──────────────────────────┐              │
│   │  reference-ui (Tailwind) │  │  reference-aui (Mud)     │              │
│   │  InteractiveServer        │  │  InteractiveAuto         │              │
│   └────────────┬──────────────┘  └────────────┬─────────────┘              │
│                └──────────────────┬───────────┘                            │
│                                   ▼                                        │
│              ┌────────────────────────────────────────┐                    │
│              │  FDW *.UI.Pages packages (PageTypes)   │                    │
│              │  + headless providers in *.Components  │                    │
│              └────────────────────┬───────────────────┘                    │
│                                   ▼                                        │
│              ┌────────────────────────────────────────┐                    │
│              │  Per-Domain API Clients (*.Clients)    │                    │
│              └────────────────────┬───────────────────┘                    │
│                                   ▼                                        │
│                            ┌─────────────┐                                 │
│                            │  REST API   │                                 │
│                            └─────────────┘                                 │
└───────────────────────────────────────────────────────────────────────────┘
```

## Quick Start

Refer to each reference repo's README for the canonical run instructions:

- `reference-ui`
- `reference-aui`

Both skins require the Reference.Api backend (from the `reference-api` repository) for authentication and configuration CRUD.

## Pages

Each FDW domain ships a `*.UI.Pages` package that contributes:

- A `[ServiceTypeOption(typeof(PageTypes), "...")]` registration carrying the page assembly and `NavDescriptor` entries
- The page implementations themselves (`@page "/route"` Razor components)
- A headless provider in the matching `*.Components` package

A skin gets that domain's pages by adding the `*.UI.Pages` package reference. The Router discovers all referenced `PageType` assemblies via `PageTypes.All()` in `Routes.razor` (see [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md)).

## Shared Logic (Headless Architecture)

Both reference UI variants are **rendering-only skins** — they contain zero business logic. All state management, API orchestration, and workflow logic lives in the FDW framework's `*.Components` packages as headless logic providers. This is the thin-client pattern applied to Blazor: the UI projects provide HTML/CSS rendering, and the framework provides everything else.

### What the Framework Provides

- **Logic providers** (per-domain `*.Components` packages) — headless Blazor components that manage state, error handling, loading states, and complex workflows via `RenderFragment<TContext>`.
- **Per-domain API clients** (`*.Clients` packages) — typed HTTP clients registered via `ApiClientTypes` TypeCollection. Application code never instantiates `HttpClient` directly.
- **Page packages** (`*.UI.Pages`) — default page implementations and the `[ServiceTypeOption(typeof(PageTypes), "...")]` registration.

### What the Reference UI Skins Provide

- HTML/CSS layout and styling (Tailwind utility classes or MudBlazor components + custom theme)
- UI-specific interactions (drag-drop, tooltips, animations)
- Blazor hosting-model configuration (`AddInteractiveServerComponents` / `AddInteractiveWebAssemblyComponents`)
- `Program.cs` wiring for the FDW packages and authentication

## Creating a New Management UI Skin

Because the skins are rendering-only, creating a new one requires no domain logic.

1. **Create a Blazor project** with your chosen hosting model and component library.
2. **Reference the FDW page packages** for the domains you want surfaced (`Fdw.Services.Connections.UI.Pages`, `Fdw.Services.Pipelines.UI.Pages`, etc.). Each transitively pulls its `*.Components` and `*.Clients` packages.
3. **Wire `Routes.razor`** to enumerate `PageTypes.All()` and pass the assemblies into `<Router AdditionalAssemblies="...">`.
4. **Wire the FDW logic** in `Program.cs` (see the reference-ui / reference-aui Program.cs for the canonical startup).
5. **Render pages** by wrapping content in the relevant headless provider — the provider exposes state and callbacks via `RenderFragment<TContext>`; your skin renders the markup.

See either reference repo for the complete shape.

## See Also

- [11-02 Blazor Hosting Models](11-02-Blazor-Hosting-Models.md) — hosting-model trade-offs
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) — provider / context / log triple
- [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md) — `PageTypes` router wiring
