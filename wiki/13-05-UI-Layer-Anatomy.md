# UI Layer Anatomy

This page shows the canonical set of object types at each layer within any domain. It is **not domain-specific** — the pattern applies identically to Connections, Data, Pipelines, Messaging, and every other domain.

For the inventory of which packages and pages exist per domain, see [16-03 UI Domain Map](16-03-UI-Domain-Map.md).

---

## Full Stack Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  UI SKIN (reference-ui, reference-ui,  …)                    │
│  Blazor hosting, routing, layout, authentication flow                           │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  PackageReference → *.UI.Pages
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  {Domain}.UI.Pages                                                              │
│  Routed pages — @page directives, page-level logging, navigation                │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  PackageReference → *.UI.Components
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  {Domain}.UI.Components                                                         │
│  Skinned components — Tailwind markup, no @page                                 │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  PackageReference → *.Components
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  {Domain}.Components                                                            │
│  Headless providers — state, loading, callbacks, no HTML                        │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  PackageReference → *.Clients
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  {Domain}.Clients                                                               │
│  HTTP client + ServiceTypeOption registration                                   │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  PackageReference → *.Clients.Abstractions
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  {Domain}.Clients.Abstractions                                                  │
│  DTOs, request/response models, optional client interface                       │
└──────────────────────────┬──────────────────────────────────────────────────────┘
                           │  HTTP (REST)
                           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  API Server — FastEndpoints, auth, validation                                   │
│  → {Domain}.Abstractions / {Domain} / {Domain}.{Implementation} (service layer) │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Per-Layer Object Types

### `{Domain}.Clients.Abstractions`

DTOs and optional client contracts. Referenced by both `*.Clients` and `*.Components`.

```
Models/
  {Entity}Dto.cs                  — response DTO (record or class)
  Create{Entity}Request.cs        — create request body
  Update{Entity}Request.cs        — update request body
  Test{Entity}Request.cs          — domain-specific request variants

I{Domain}ApiClient.cs             — optional: interface for the API client
                                    (only needed when cross-assembly mocking or
                                     contract sharing is required)
```

Rules:
- DTOs are plain records/classes — no business logic, no service references.
- Collections are `IReadOnlyList<T>` or `IEnumerable<T>`, never `List<T>`.
- Cross-domain shared contracts go in `Web.Clients.Abstractions/Contracts/` as interfaces (`IColumnSchema`, `IDataPreviewRequest`, etc.).

---

### `{Domain}.Clients`

Concrete HTTP client and the `ApiClientTypes` registration hook. One `*ApiClient.cs` + one `*ClientType.cs`.

```
{Domain}ApiClient.cs              — extends ApiClientBase
                                    uses named HttpClient (CreateClient("{Domain}Client"))
                                    one method per endpoint: GetAll, GetById, Create, Update, Delete
                                    virtual methods (for Moq testability)

Registration/
  {Domain}ClientType.cs           — [ServiceTypeOption(typeof(ApiClientTypes), "{Domain}Client")]
                                    Configure() calls services.Add{Domain}ApiClient(baseUrl)
                                                              .AddBearerTokenHandler()
```

Rules:
- No typed DI (`services.AddHttpClient<T>()`). Always named: `services.AddHttpClient("{Domain}Client", …)`.
- `Configure()` is the only place the named client gets registered — it runs during the three-phase `ApiClientTypes` lifecycle.
- `{Domain}ApiClient` is not `sealed` so test projects can `Mock<{Domain}ApiClient>()`.

---

### `{Domain}.Components`

Headless provider triple: `Provider.razor` + `Context.cs` + `ProviderLog.cs`. Zero HTML.

```
Components/{Feature}/
  {Feature}Provider.razor         — headless Blazor component
                                    [Inject] {Domain}ApiClient Api
                                    RenderFragment<{Feature}Context> ChildContent
                                    state: _items, _isLoading, _errorMessage
                                    lifecycle: OnInitialized (NullLogger), OnAfterRenderAsync (load)
                                    public methods: Load…(), Create…(), Update…(), Delete…()

  {Feature}Context.cs             — sealed, immutable snapshot of provider state
                                    IReadOnlyList<T> Items { get; init; }
                                    bool IsLoading { get; init; }
                                    string? ErrorMessage { get; init; }
                                    Func<Task> OnRefresh { get; init; } = () => Task.CompletedTask
                                    Func<T, Task> OnSelected { get; init; } = _ => Task.CompletedTask

Logging/
  {Feature}ProviderLog.cs         — [MessageLogging] source-generated log class
                                    Loading{Items}  Trace
                                    Loaded{Items}   Information  (with count)
                                    Cache hit/miss  Trace        (if caching)
                                    Load{Items}Failed  Error     (with Exception)
```

Rules:
- No `@` HTML in providers — only `@ChildContent(_context)`.
- `_context` rebuilt via `RebuildContext()` before every `StateHasChanged()`.
- `ILogger<T>` injected as optional `LoggerParam`; always falls back to `NullLogger<T>.Instance`.
- Every async method accepts `CancellationToken cancellationToken = default`.
- `IGenericResult` checked with `if (!result.IsSuccess)` before accessing `.Value`.

---

### `{Domain}.UI.Components`

Skinned Tailwind/MudBlazor components. No `@page` directive. Reusable within a skin.

```
{Entity}List.razor                — renders a list/table of {Entity}Dto items
                                    [Parameter] IReadOnlyList<{Entity}Dto> Items
                                    [Parameter] EventCallback<{Entity}Dto> OnSelect
                                    [Parameter] string AccentColor = "text-red-500"  (parameterized)

{Entity}Card.razor                — summary card / grid tile
{Entity}Dialog.razor              — create/edit modal or panel
{Entity}StatusBadge.razor         — inline status chip

_Imports.razor                    — @namespace FractalDataWorks.{Domain}.UI.Components
                                    @using …                     (all types used in this pkg)

Logging/
  {Feature}Log.cs                 — page-level [MessageLogging] shared across components in pkg
```

Rules:
- No `@inject` of API clients — components receive data via `[Parameter]`.
- Color classes are `[Parameter]` with defaults matching the reference skin.
- Components in `*.UI.Components` may reference headless providers via `[CascadingParameter]` but do not own providers.

---

### `{Domain}.UI.Pages`

Routed pages. Each page owns navigation, UI-only state (wizard step, open dialog), and snackbar/toast triggers.

```
Pages/
  {Entity}s.razor                 — @page "/{entities}"
                                    wraps <{Feature}Provider> or <{Feature}Provider>
                                    calls ctx.Load…(), ctx.OnRefresh
                                    owns _selectedItem, _dialogOpen (UI-only state)
                                    no @inject ApiClient

  {Entity}Detail.razor            — @page "/{entities}/{id:guid}"
  {Wizard}.razor                  — @page "/{entities}/new"

  {SubDomain}/                    — subdirectory for grouped sub-pages
    Index.razor
    Detail.razor

Logging/
  {Page}Log.cs                    — [MessageLogging] for page-level events
                                    (navigation, wizard step changes, dialog open/close)

AssemblyAttributes.cs             — [assembly: DefaultUiPagesAssembly]
                                    enables automatic router discovery in reference-ui

_Imports.razor                    — @namespace FractalDataWorks.{Domain}.UI.Pages
                                    @using …                     (all types used in this pkg)
```

Rules:
- Pages may inject `NavigationManager`, `[CascadingParameter] MainLayoutBase?` (for `SetNoPadding`), and auth services.
- Pages do NOT inject `*ApiClient` directly — all data flows through providers.
- Pages do NOT own `IsLoading` or `ErrorMessage` — providers own those.
- One `.razor` file per page (FDW005).
- Every FDW default page package carries `[assembly: DefaultUiPagesAssembly]`. The skin's `Routes.razor` scans referenced assemblies for this attribute to populate `AdditionalAssemblies`. Alternative skins that replace domain pages omit the attribute from their own packages. See [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md).

---

## API Service Layer (for completeness)

The API layer has its own three-project structure, separate from the UI layers above.

```
{Domain}.Abstractions             — netstandard2.0
  I{Domain}.cs                    — service interface (public API surface)
  I{Domain}Configuration.cs       — configuration interface
  {Domain}ConfigurationBase.cs    — abstract config base class
  I{Domain}Factory.cs             — factory interface
  I{Domain}Provider.cs            — provider interface
  {Domain}TypeBase.cs             — CRTP base: ServiceTypeBase<I{Domain}, I{Domain}Factory>

{Domain}                          — net10.0 (core/collection package)
  {Domain}Types.cs                — [ServiceTypeCollection] — three-phase DI lifecycle
                                    GenerateProvider = true → Default{Domain}Provider
  Default{Domain}Provider.cs      — default provider implementation
  Logging/
    {Domain}Log.cs                — [MessageLogging] for service operations

{Domain}.{Implementation}         — net10.0 (e.g., MsSql, Webhook, EnvSecrets)
  {Impl}{Domain}Type.cs           — [ServiceTypeOption(typeof({Domain}Types), "{Impl}")]
                                    Configure() → binds IOptions<{Impl}Configuration>
                                    Register()  → registers factory
  {Impl}{Domain}Factory.cs        — creates IService instances from configuration
  {Impl}{Domain}Configuration.cs  — [ManagedConfiguration] — maps to SQL table
  Logging/
    {Impl}Log.cs                  — [MessageLogging] for implementation operations
```

---

## Layer Responsibility Summary

| Layer | Owns | Does NOT own |
|-------|------|--------------|
| `*.Clients.Abstractions` | DTOs, request/response shapes, optional `IApiClient` | HTTP logic, state, UI |
| `*.Clients` | HTTP calls, named client registration, `ApiClientTypes` hook | State, rendering, business logic |
| `*.Components` | State, loading, error, callbacks (RenderFragment) | HTML, CSS, navigation |
| `*.UI.Components` | Tailwind/MudBlazor markup, parameterized colors | API calls, state ownership, routing |
| `*.UI.Pages` | `@page` routes, UI-only state, navigation, snackbar | API calls, loading/error state |
| API service layer | Domain logic, data access, configuration | HTTP transport, UI |

---

## See Also

- [16-03 UI Domain Map](16-03-UI-Domain-Map.md) — per-domain package and page inventory
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) — provider structural contract and hard rules
- [16-01 API Clients Reference](16-01-API-Clients-Reference.md) — all `*.Clients` packages
- [06-02 Creating a Service Domain](06-02-Creating-Service-Domain.md) — API service layer creation guide
