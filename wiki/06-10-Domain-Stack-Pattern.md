# 06-10 Domain Stack Pattern

Every FDW service domain follows a consistent six-layer stack from database to UI. This document defines the canonical structure that all domains must implement.

## Stack Overview

```
┌─────────────────────────────────────────────────────────┐
│  Page         (@page route, thin MudBlazor shell)       │  *.UI.Pages
├─────────────────────────────────────────────────────────┤
│  Provider     (headless, RenderFragment<Context>)       │  *.Components
│  Context      (immutable state snapshot + callbacks)    │
│  Model        (mutable form data, optional)             │
├─────────────────────────────────────────────────────────┤
│  ApiClient    (typed HTTP, defines all routes)          │  *.Clients
├─────────────────────────────────────────────────────────┤
│  Closure      (sealed, Tags only, in ref-api)           │  Reference.Api
├─────────────────────────────────────────────────────────┤
│  EndpointBase (abstract, all business logic)            │  *.Endpoints
│  EndpointLog  (MessageLogging companion)                │
├─────────────────────────────────────────────────────────┤
│  Service      (domain logic, data access)               │  Services.{Domain}
│  Configuration (ManagedConfiguration, IOptions)         │  Services.{Domain}
└─────────────────────────────────────────────────────────┘
```

## Layer 1: Endpoint Base (FDW)

**Project:** `Fdw.Services.{Domain}.Endpoints` or `FractalDataWorks.{Domain}.Endpoints`

**File:** `{Action}{Resource}EndpointBase.cs`

All endpoint logic lives here. The base class handles routing, authorization, OpenAPI metadata, request handling, error mapping, and logging.

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.{Domain}.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.{Domain}.Endpoints;

/// <summary>
/// Abstract base for listing {resource} with optional filtering.
/// Route: GET /{resource}
/// </summary>
public abstract class List{Resource}EndpointBase
    : EndpointWithoutRequest<IReadOnlyList<{Resource}SummaryDto>>
{
    private readonly I{Resource}Provider _{resource}Provider;
    private readonly ILogger<List{Resource}EndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="List{Resource}EndpointBase"/> class.
    /// </summary>
    /// <param name="{resource}Provider">The {resource} provider.</param>
    /// <param name="logger">The logger instance.</param>
    protected List{Resource}EndpointBase(
        I{Resource}Provider {resource}Provider,
        ILogger<List{Resource}EndpointBase> logger)
    {
        _{resource}Provider = {resource}Provider;
        _logger = logger ?? NullLogger<List{Resource}EndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/{resource}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("fdw:{resource}:read");
#endif
        Summary(s =>
        {
            s.Summary = "List {resource}";
            s.Description = "Returns all configured {resource}.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override to add Tags or customize.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        {Domain}EndpointLog.ListingResources(_logger);

        try
        {
            var result = await _{resource}Provider.GetAll(ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                {Domain}EndpointLog.ListResourcesFailed(_logger);
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            {Domain}EndpointLog.ResourcesListed(_logger, result.Value?.Count ?? 0);
            await SendOkAsync(result.Value ?? [], ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            {Domain}EndpointLog.ListResourcesException(_logger, ex);
            HttpContext.Response.StatusCode = 500;
        }
        catch (Exception ex)
        {
            {Domain}EndpointLog.ListResourcesException(_logger, ex);
            HttpContext.Response.StatusCode = 500;
        }
    }
}
```

### CRUD Endpoint Naming Convention

| Operation | Class Name | HTTP | Route |
|-----------|-----------|------|-------|
| List all | `List{Resource}EndpointBase` | GET | `/{resource}` |
| Get one | `Get{Resource}EndpointBase` | GET | `/{resource}/{Name}` |
| Create | `Create{Resource}EndpointBase` | POST | `/{resource}` |
| Update | `Update{Resource}EndpointBase` | PUT | `/{resource}/{Name}` |
| Delete | `Delete{Resource}EndpointBase` | DELETE | `/{resource}/{Name}` |
| Custom action | `{Action}{Resource}EndpointBase` | POST | `/{resource}/{Name}/{action}` |

### CRUD Base Classes

For standard CRUD, prefer inheriting from the FDW CRUD base classes:

| Base Class | Purpose |
|-----------|---------|
| `CrudListEndpoint<TResponse>` | GET list with ETag, pagination |
| `CrudGetEndpoint<TRequest, TResponse>` | GET by name with ETag, 404 handling |
| `CrudCreateEndpoint<TRequest, TResponse>` | POST with conflict detection |
| `CrudUpdateEndpoint<TRequest, TResponse>` | PUT with ETag, 404 handling |
| `CrudDeleteEndpoint<TRequest>` | DELETE with 404 handling |

These provide routing, auth, ETag, error mapping, and virtual hooks:
- `ConfigureEndpoint()` — add Tags, customize config
- `OnBeforeGet/Create/Update/Delete(identifier)` — pre-operation logging
- `OnAfterGet/Create/Update/Delete(identifier)` — post-operation logging
- `OnNotFound(identifier)` — not-found logging

## Layer 2: Endpoint Log (FDW)

**File:** `Logging/{Domain}EndpointLog.cs` in the same Endpoints project.

```csharp
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.{Domain}.Endpoints.Logging;

/// <summary>
/// MessageLogging for {Domain} endpoint operations.
/// EventId range: {NNNN}-{NNNN}
/// </summary>
public static partial class {Domain}EndpointLog
{
    // List operations
    [MessageLogging(EventId = {N}00, Level = LogLevel.Trace,
        Message = "{Domain}: Listing resources")]
    public static partial IGenericMessage ListingResources(ILogger logger);

    [MessageLogging(EventId = {N}01, Level = LogLevel.Information,
        Message = "{Domain}: Listed {count} resources")]
    public static partial IGenericMessage ResourcesListed(ILogger logger, int count);

    [MessageLogging(EventId = {N}02, Level = LogLevel.Warning,
        Message = "{Domain}: Failed to list resources")]
    public static partial IGenericMessage ListResourcesFailed(ILogger logger);

    [MessageLogging(EventId = {N}03, Level = LogLevel.Error,
        Message = "{Domain}: Exception listing resources")]
    public static partial IGenericMessage ListResourcesException(
        ILogger logger, Exception exception);

    // Get operations
    [MessageLogging(EventId = {N}10, Level = LogLevel.Trace,
        Message = "{Domain}: Fetching resource '{name}'")]
    public static partial IGenericMessage FetchingResource(ILogger logger, string name);

    [MessageLogging(EventId = {N}11, Level = LogLevel.Information,
        Message = "{Domain}: Resource '{name}' retrieved")]
    public static partial IGenericMessage ResourceRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = {N}12, Level = LogLevel.Warning,
        Message = "{Domain}: Resource '{name}' not found")]
    public static partial IGenericMessage ResourceNotFound(ILogger logger, string name);

    // Create operations ({N}20-{N}29)
    // Update operations ({N}30-{N}39)
    // Delete operations ({N}40-{N}49)
    // Domain-specific operations ({N}50+)
}
```

**EventId convention:** Each domain gets a 100-ID block. Within that block:
- `{N}00-{N}09` — List
- `{N}10-{N}19` — Get
- `{N}20-{N}29` — Create
- `{N}30-{N}39` — Update
- `{N}40-{N}49` — Delete
- `{N}50-{N}99` — Domain-specific actions

## Layer 3: Endpoint Closure (Reference App)

**Project:** `Reference.Api` (or any consuming application)

**File:** `Endpoints/{Domain}/{Action}{Resource}Endpoint.cs`

The closure seals the generic base, passes DI dependencies, and adds Scalar Tags. **No business logic here.**

```csharp
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.{Domain}.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Closure for the list {resource} endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class List{Resource}Endpoint : List{Resource}EndpointBase
{
    /// <inheritdoc />
    public List{Resource}Endpoint(
        I{Resource}Provider {resource}Provider,
        ILogger<List{Resource}EndpointBase> logger)
        : base({resource}Provider, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("{Domain}");
    }
}
```

**Rules:**
- Always `sealed`
- Always `[ExcludeFromCodeCoverage]`
- No `HandleAsync` override
- No business logic
- Tags in `ConfigureEndpoint()` for Scalar grouping

## Layer 4: API Client (FDW)

**Project:** `Fdw.Services.{Domain}.Clients`

**File:** `{Domain}ApiClient.cs`

Typed HTTP client that defines all routes. Used by UI providers.

```csharp
using Fdw.Results;
using Fdw.Web.Clients;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.{Domain}.Clients;

/// <summary>
/// HTTP API client for {Domain} operations.
/// </summary>
public class {Domain}ApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="{Domain}ApiClient"/> class.
    /// </summary>
    public {Domain}ApiClient(HttpClient httpClient, ILogger<{Domain}ApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>Lists all {resource}.</summary>
    public virtual Task<IGenericResult<IReadOnlyList<{Resource}SummaryDto>>> GetAll(
        CancellationToken ct = default)
        => Get<IReadOnlyList<{Resource}SummaryDto>>("{resource}", ct);

    /// <summary>Gets a {resource} by name.</summary>
    public virtual Task<IGenericResult<{Resource}DetailDto>> Get(
        string name, CancellationToken ct = default)
        => Get<{Resource}DetailDto>($"{resource}/{name}", ct);

    /// <summary>Creates a new {resource}.</summary>
    public virtual Task<IGenericResult<{Resource}DetailDto>> Create(
        Create{Resource}Request request, CancellationToken ct = default)
        => Post<Create{Resource}Request, {Resource}DetailDto>("{resource}", request, ct);

    /// <summary>Updates an existing {resource}.</summary>
    public virtual Task<IGenericResult<{Resource}DetailDto>> Update(
        string name, Update{Resource}Request request, CancellationToken ct = default)
        => Put<Update{Resource}Request, {Resource}DetailDto>($"{resource}/{name}", request, ct);

    /// <summary>Deletes a {resource}.</summary>
    public virtual Task<IGenericResult> Delete(
        string name, CancellationToken ct = default)
        => Delete($"{resource}/{name}", ct);
}
```

**Rules:**
- Routes in client MUST match endpoint routes exactly
- Methods are `virtual` for Moq testability
- Use `Uri.EscapeDataString()` for path parameters
- Query string parameters for filtering

## Layer 5: Provider (FDW)

**Project:** `Fdw.Services.{Domain}.Components`

**File:** `Components/{Feature}/{Feature}Provider.razor`

Headless Blazor component. Makes API calls, manages state, exposes immutable context to consumers.

```razor
@namespace Fdw.Services.{Domain}.Components
@using Microsoft.AspNetCore.Components
@using Microsoft.Extensions.Logging
@using Microsoft.Extensions.Logging.Abstractions

@if (ChildContent is not null)
{
    @ChildContent(_context)
}

@code {
    [Parameter] public RenderFragment<{Feature}Context>? ChildContent { get; set; }
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILoggerFactory? LoggerFactory { get; set; }

    private {Domain}ApiClient? _api;
    private ILogger<{Feature}Provider> _logger = NullLogger<{Feature}Provider>.Instance;
    private {Feature}Context _context = new();
    private bool _initialized;

    // ── State ──────────────────────────────────────────
    private IReadOnlyList<{Resource}SummaryDto> _items = [];
    private bool _isLoading;
    private string? _errorMessage;

    // ── Lifecycle ──────────────────────────────────────
    protected override void OnInitialized()
    {
        _api = new {Domain}ApiClient(
            HttpClientFactory.CreateClient("{Domain}Client"),
            LoggerFactory?.CreateLogger<{Domain}ApiClient>()!);
        _logger = LoggerFactory?.CreateLogger<{Feature}Provider>()
            ?? NullLogger<{Feature}Provider>.Instance;
        RebuildContext();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            await LoadItems();
        }
    }

    // ── Private Operations ─────────────────────────────
    private async Task LoadItems(CancellationToken ct = default)
    {
        _isLoading = true;
        RebuildContext();
        StateHasChanged();

        try
        {
            var result = await _api!.GetAll(ct);
            if (!result.IsSuccess)
            {
                _errorMessage = "{Feature}Provider: Failed to load items";
                _items = [];
                return;
            }
            _items = result.Value?.ToList() ?? [];
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _items = [];
        }
        finally
        {
            _isLoading = false;
            RebuildContext();
            StateHasChanged();
        }
    }

    // ── Context Builder ────────────────────────────────
    private void RebuildContext()
    {
        _context = new {Feature}Context
        {
            Items = _items,
            IsLoading = _isLoading,
            ErrorMessage = _errorMessage,
            OnRefresh = () => LoadItems(),
            // ... other callbacks
        };
    }
}
```

### Context Object

**File:** `Components/{Feature}/{Feature}Context.cs`

Immutable snapshot passed to the consumer RenderFragment. Contains state and callbacks.

```csharp
namespace Fdw.Services.{Domain}.Components;

/// <summary>
/// Immutable context for the <see cref="{Feature}Provider"/>.
/// Carries state snapshots and callback delegates.
/// </summary>
public sealed class {Feature}Context
{
    // ── Data State ──────────────────────────────
    /// <summary>Gets the loaded items.</summary>
    public IReadOnlyList<{Resource}SummaryDto> Items { get; init; } = [];

    // ── Loading / Error State ───────────────────
    /// <summary>Gets whether data is loading.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Gets the most recent error message.</summary>
    public string? ErrorMessage { get; init; }

    // ── Callbacks ───────────────────────────────
    /// <summary>Refreshes the item list.</summary>
    public Func<Task> OnRefresh { get; init; } = () => Task.CompletedTask;
}
```

### Form Model (optional, for wizards/editors)

**File:** `Components/{Feature}/{Feature}Model.cs`

Mutable model bound to form inputs. Separate from Context to keep Context immutable.

```csharp
namespace Fdw.Services.{Domain}.Components;

/// <summary>
/// Mutable form model for the {Feature} editor.
/// </summary>
public sealed class {Feature}Model
{
    /// <summary>Gets or sets the resource name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
```

## Layer 6: Page (FDW)

**Project:** `Fdw.Services.{Domain}.UI.Pages`

**File:** `Pages/{Feature}.razor`

Thin shell that composes a Provider with MudBlazor markup. **No logic here.**

```razor
@page "/{feature}"

<PageTitle>{Feature}</PageTitle>

<{Feature}Provider>
    <ChildContent Context="ctx">
        @if (ctx.IsLoading)
        {
            <MudProgressLinear Indeterminate />
        }
        else if (ctx.ErrorMessage is not null)
        {
            <MudAlert Severity="Severity.Error">@ctx.ErrorMessage</MudAlert>
        }
        else
        {
            <MudTable Items="ctx.Items" Hover Dense>
                <HeaderContent>
                    <MudTh>Name</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Name</MudTd>
                </RowTemplate>
            </MudTable>
        }
    </ChildContent>
</{Feature}Provider>
```

**Rules:**
- Pages are in separate `*.UI.Pages` projects (NuGet packages)
- No `@code` blocks with business logic
- No direct API client usage — only through Provider context
- MudBlazor components only — no raw HTML where a MudBlazor component exists
- Reference-ui composes these page packages — it doesn't define domain pages

## Project Structure Summary

For a domain called `Widgets`:

```
FractalDataWorks/public/src/
├── Fdw.Services.Widgets.Abstractions/     # netstandard2.0
│   ├── IWidget.cs                                       # Domain interface
│   ├── IWidgetProvider.cs                               # Provider interface
│   └── IWidgetConfiguration.cs                          # Configuration interface
│
├── Fdw.Services.Widgets/                   # net10.0
│   ├── WidgetTypes.cs                                   # ServiceTypeCollection
│   ├── DefaultWidgetProvider.cs                         # Provider implementation
│   └── Logging/WidgetLog.cs                             # Domain MessageLogging
│
├── Fdw.Services.Widgets.Clients/           # net10.0
│   ├── WidgetApiClient.cs                               # Typed HTTP client
│   └── Models/                                          # Client DTOs
│
├── Fdw.Services.Widgets.Clients.Abstractions/  # netstandard2.0
│   └── Models/                                          # Shared DTO interfaces
│
├── Fdw.Services.Widgets.Endpoints/         # net10.0
│   ├── ListWidgetsEndpointBase.cs
│   ├── GetWidgetEndpointBase.cs
│   ├── CreateWidgetEndpointBase.cs
│   ├── UpdateWidgetEndpointBase.cs
│   ├── DeleteWidgetEndpointBase.cs
│   └── Logging/WidgetEndpointLog.cs
│
├── Fdw.Services.Widgets.Components/        # net10.0
│   └── Components/Widgets/
│       ├── WidgetProvider.razor
│       ├── WidgetContext.cs
│       ├── WidgetEditorProvider.razor
│       ├── WidgetEditorContext.cs
│       └── WidgetEditorModel.cs
│
└── Fdw.Services.Widgets.UI.Pages/          # net10.0
    └── Pages/
        ├── Widgets.razor                                # List page
        ├── WidgetDetail.razor                           # Detail page
        └── WidgetEditor.razor                           # Create/Edit page
```

## Anti-Patterns

| Wrong | Right |
|-------|-------|
| Business logic in endpoint closure | All logic in EndpointBase |
| Raw `ILogger` calls in endpoints | MessageLogging companion class |
| API client calls in Page razor | API calls only in Provider |
| `@code` logic in Page | Logic in Provider, expose via Context |
| Mutable Context properties | Context is immutable (`init` only), Model is mutable |
| Multiple providers sharing state | Each Provider owns its state independently |
| Concrete DTOs in Abstractions | Interfaces in Abstractions, concrete DTOs in Clients |
| `Endpoint<>` in reference-app | Always extend FDW EndpointBase |

## Checklist for New Domains

- [ ] Abstractions project with interfaces
- [ ] Service project with provider + TypeCollection
- [ ] Clients project with ApiClient + DTOs
- [ ] Endpoints project with abstract bases + MessageLogging
- [ ] Components project with Provider + Context (+ Model if editor)
- [ ] UI.Pages project with Page razor files
- [ ] Closures in reference-app with Tags
- [ ] Postman collection for all endpoints
- [ ] bUnit tests for all providers
- [ ] EventId range allocated in RESULTCODE-CATALOG.md
