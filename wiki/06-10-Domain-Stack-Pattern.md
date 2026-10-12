# 06-10 Domain Stack Pattern

Every FDW service domain follows a consistent stack from the database to the typed API client. This document defines the canonical structure that all domains must implement.

Screens are not part of the domain stack. The web console (`reference-react-ui`) calls the endpoints over HTTP, and the terminal UI calls the `*.Clients` packages. See [11-01 Management UI Overview](11-01-Management-UI-Overview.md). The Blazor `*.Components` and `*.UI.Pages` layers were removed in FDW-872.

## Stack Overview

```
┌─────────────────────────────────────────────────────────┐
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

Typed HTTP client that defines all routes. Used by .NET consumers: the terminal UI and host-to-host calls.

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
└── Fdw.Services.Widgets.Endpoints/         # net10.0
    ├── ListWidgetsEndpointBase.cs
    ├── GetWidgetEndpointBase.cs
    ├── CreateWidgetEndpointBase.cs
    ├── UpdateWidgetEndpointBase.cs
    ├── DeleteWidgetEndpointBase.cs
    └── Logging/WidgetEndpointLog.cs
```

## Anti-Patterns

| Wrong | Right |
|-------|-------|
| Business logic in endpoint closure | All logic in EndpointBase |
| Raw `ILogger` calls in endpoints | MessageLogging companion class |
| Concrete DTOs in Abstractions | Interfaces in Abstractions, concrete DTOs in Clients |
| `Endpoint<>` in reference-app | Always extend FDW EndpointBase |

## Checklist for New Domains

- [ ] Abstractions project with interfaces
- [ ] Service project with provider + TypeCollection
- [ ] Clients project with ApiClient + DTOs
- [ ] Endpoints project with abstract bases + MessageLogging
- [ ] Closures in reference-app with Tags
- [ ] Postman collection for all endpoints
- [ ] EventId range allocated in RESULTCODE-CATALOG.md
