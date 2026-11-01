# The ui domain

UI: the UI abstractions and services, components (Razor console, TUI), Spectre rendering, themes, and the lineage and schema UI client abstractions.

This folder, `public/src/ui/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/ui/`.

## Projects

fractaldataworks:

- `Fdw.UI`
- `Fdw.UI.Abstractions`: Pure C# abstractions for building type-safe UI components using CRTP. Framework-agnostic foundation that works with any UI technology.
- `Fdw.UI.Components`: Backend-agnostic UI data models for configuration forms. Includes TextInput, Select, MultiSelect, Numeric, DatePicker, Checkbox, Table, and Form models.
- `Fdw.UI.Components.RazorConsole`: Razor syntax for terminal UIs that compiles to Spectre.Console. Declarative terminal UI framework.
- `Fdw.UI.Components.RazorConsole.SourceGenerators`
- `Fdw.UI.Components.TUI`: Terminal User Interface implementation of Fdw UI components using Spectre.Console.
- `Fdw.UI.Lineage.Clients.Abstractions`: Data lineage graph UI DTOs for Fdw Management UIs.
- `Fdw.UI.Rendering.Spectre`: Spectre.Console rendering backend for Fdw UI. Provides terminal UI prompts, tables, and panels for configuration forms.
- `Fdw.UI.Schema.Clients.Abstractions`: Schema visualization UI DTOs for Fdw Management UIs.
- `Fdw.UI.Services`
- `Fdw.UI.Themes`: Theme TypeCollections (ColorPalettes, BorderStyles, IconSets, MenuThemes) for Fdw UI
- `Fdw.UI.Themes.Abstractions`: Interfaces and base classes for Fdw UI theming (color palettes, border styles, icon sets, menu themes)
- `Fdw.UI.Themes.Clients`: API clients and DTOs for theme management operations.
- `Fdw.UI.Themes.Endpoints`: HTTP endpoints for the Themes domain, extracted from Fdw.Web.Api.
- `Fdw.UI.Themes.Registration`: The UI theme service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `Fdw.UI.Web.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IMenuTheme` (Fdw.UI.Themes.Abstractions)
- `ComponentTypeBase` (Fdw.UI.Web.Abstractions)
- `ChartTypeBase` (Fdw.UI.Abstractions)
- `IInputComponentModel` (Fdw.UI.Abstractions)
- `LineageEdgeTypeBase` (Fdw.UI.Lineage.Clients.Abstractions)
- `LineageNodeTypeBase` (Fdw.UI.Lineage.Clients.Abstractions)
- `LineageNodeStatusBase` (Fdw.UI.Lineage.Clients.Abstractions)
- `IComponentModel` (Fdw.UI.Abstractions)
- `ValidationSeverityBase` (Fdw.UI.Abstractions)
- `ChartEncodingRoleBase` (Fdw.UI.Abstractions)
- `IPageAction` (Fdw.UI.Abstractions)
- `IRenderMode` (Fdw.UI.Abstractions)
- `IRenderMode` (Fdw.UI.Web.Abstractions)
- `IPageModel` (Fdw.UI.Abstractions)
- `IRenderContext` (Fdw.UI.Abstractions)
- `ITreeNode` (Fdw.UI.Abstractions)
- `CanvasNodeTypeBase` (Fdw.UI.Abstractions)
- `IValidationSeverity` (Fdw.UI.Abstractions)
- `IComponentType` (Fdw.UI.Web.Abstractions)
- `IListPageModel` (Fdw.UI.Abstractions)
- and 88 more in the `.Abstractions` projects above

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/ui/Fdw.UI.Components.TUI.Tests/Fdw.UI.Components.TUI.Tests.csproj -c Release`
- `dotnet test public/tests/ui/Fdw.UI.Tests/Fdw.UI.Tests.csproj -c Release`
- `dotnet test public/tests/ui/Fdw.UI.Themes.Clients.Tests/Fdw.UI.Themes.Clients.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (20), `messagelogging` (10), `pipelines` (8), `data` (6), `configuration` (5), `abstractions` (4), `endpoints` (4), `results` (4), `services` (4), `authentication` (3), `authorization` (2), `connections` (2), `datasets` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `users` (2), `validation` (2), `calculations` (1), `catalog` (1), `commands` (1), `hosts` (1), `multitenancy` (1), `quality` (1), `registration` (1), `roslyn` (1), `security` (1).
Depended on by: `shells` (3), `endpoints` (2), `authentication` (1), `authorization` (1), `catalog` (1), `connections` (1), `data` (1), `hosts` (1), `multitenancy` (1), `pipelines` (1), `quality` (1), `scheduling` (1), `schema` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `ui-host`.
