# UI Layer Anatomy

This page shows the layers between a domain's API and the screens that draw it. The pattern is the same for every domain: Connections, Data, Pipelines, Messaging and the rest.

FDW ships no web UI. The web console is the separate `reference-react-ui` repository, which calls the API over HTTP. FDW supplies the API, the typed .NET clients, and framework-neutral page and form models that the terminal UI draws. The Blazor packages (`*.Components`, `*.UI.Components`, `*.UI.Pages`, `Fdw.UI.Components.Blazor`) were removed in FDW-872.

---

## Full Stack Diagram

```
┌──────────────────────────────────┐    ┌──────────────────────────────────────────┐
│  reference-react-ui              │    │  Fdw.TUI.Management (terminal)           │
│  React + TypeScript console      │    │  draws Fdw.UI.Components page/form       │
│  own endpoint table              │    │  models through Fdw.UI.Rendering.Spectre │
│  (src/core/endpoints.ts)         │    └────────────────────┬─────────────────────┘
└────────────────┬─────────────────┘                         │  method calls
                 │                                           ▼
                 │                      ┌──────────────────────────────────────────┐
                 │                      │  {Domain}.Clients                        │
                 │                      │  named HttpClient, ApiClientTypes hook   │
                 │                      └────────────────────┬─────────────────────┘
                 │                                           │  DTOs
                 │                      ┌────────────────────▼─────────────────────┐
                 │                      │  {Domain}.Clients.Abstractions           │
                 │                      │  request/response DTOs                   │
                 │                      └────────────────────┬─────────────────────┘
                 │  HTTP + bearer token                      │  HTTP + bearer token
                 ▼                                           ▼
┌───────────────────────────────────────────────────────────────────────────────────┐
│  API host (reference-api) closing the FDW *.Endpoints base classes                │
│  → domain services → DataGateway                                                  │
└───────────────────────────────────────────────────────────────────────────────────┘
```

The React console does not use the .NET `*.Clients` packages. It keeps its own table of every route it calls. The `*.Clients` packages serve .NET consumers: the TUI, and hosts that call another host's API (reference-api and reference-scheduler reference them).

---

## Per-Layer Object Types

### `{Domain}.Clients.Abstractions`

DTOs and optional client contracts. Referenced by `*.Clients` and by any .NET consumer that shares the DTO shapes.

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
  {Domain}ClientType.cs           — [Implementation(typeof(ApiClientTypes), "{Domain}Client")]
                                    Configure() calls services.Add{Domain}ApiClient(baseUrl)
                                                              .AddBearerTokenHandler()
```

Rules:
- No typed DI (`services.AddHttpClient<T>()`). Always named: `services.AddHttpClient("{Domain}Client", …)`.
- `Configure()` is the only place the named client gets registered — it runs during the three-phase `ApiClientTypes` lifecycle.
- `{Domain}ApiClient` is not `sealed` so test projects can `Mock<{Domain}ApiClient>()`.

---

### `Fdw.UI.Components` (framework-neutral models)

Plain `Microsoft.NET.Sdk` library with no Razor or ASP.NET dependency. A renderer draws these models; the models hold no rendering code.

```
Pages/        — ListPageModel, DetailPageModel, DashboardPageModel, TreePageModel,
                WizardPageModel + WizardStep, LogViewerPageModel, PageAction, ...
Models/       — form field models: TextInputModel, SelectModel, CheckboxModel,
                DatePickerModel, NumericInputModel, MultiSelectModel, ...
Icons/        — icon glyphs
Services/     — status badge and colour variants
TypeIntegration/ — select model built from a TypeCollection
```

Today the terminal renderer (`Fdw.UI.Rendering.Spectre`, used by `Fdw.TUI.Management`) is the consumer. The React console does not use these models.

---

## API Service Layer

The API layer has its own three-project structure. Every UI reaches it over HTTP.

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
  {Impl}{Domain}Type.cs           — [Implementation(typeof({Domain}Types), "{Impl}")]
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
| `Fdw.UI.Components` | Page and form models a renderer draws | Rendering, HTTP, state |
| `reference-react-ui` | Web screens, routing, its own HTTP calls | Domain logic, data access |
| API service layer | Domain logic, data access, configuration | HTTP transport, UI |

---

## See Also

- [11-01 Management UI Overview](11-01-Management-UI-Overview.md): the web console and the terminal UI
- [16-01 API Clients Reference](16-01-API-Clients-Reference.md): all `*.Clients` packages
- [06-02 Creating a Service Domain](06-02-Creating-Service-Domain.md): API service layer creation guide
