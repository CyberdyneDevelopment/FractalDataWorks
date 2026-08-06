# 12-07 API Endpoints


FractalDataWorks provides a three-tier API endpoint architecture built on [FastEndpoints](https://fast-endpoints.com/). Endpoints are distributed as **15 per-domain NuGet packages**, each containing generic base endpoint classes and shared DTOs. Consumer projects create thin closures that close generic type parameters with concrete types.

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│  Tier 3: Consumer Closures (Reference.Api/Endpoints/)               │
│  - Thin sealed classes closing generic type parameters               │
│  - Supplies concrete configuration types (e.g., MsSqlConfig)        │
│  - Implements abstract mapping methods                               │
│  - Optionally adds logging hooks                                     │
├──────────────────────────────────────────────────────────────────────┤
│  Tier 2: Per-Domain Endpoint Packages (15 .Endpoints packages)      │
│  - Generic base classes with open type parameters                    │
│  - Shared DTOs for each domain                                       │
│  - Business logic, data access, virtual hooks                        │
├──────────────────────────────────────────────────────────────────────┤
│  Tier 1: Abstract CRUD Bases (Fdw.Web.RestEndpoints)   │
│  - CrudListEndpoint, CrudGetEndpoint, CrudCreateEndpoint            │
│  - CrudUpdateEndpoint, CrudDeleteEndpoint                           │
│  - Security bases: AdminEndpointBase, AuthenticatedEndpointBase,    │
│    ProtectedEndpointBase, PublicEndpointBase                        │
│  - Handles routing, auth, error handling, lifecycle hooks            │
└──────────────────────────────────────────────────────────────────────┘
         │                    │
         ▼                    ▼
   Contract Types        EndpointLogger
   (Web.Endpoints)       (Web.RestEndpoints)
```

## Package Structure

| Package | Purpose | Contents |
|---------|---------|----------|
| `Fdw.Web.Endpoints` | Contract types | `INamedResource`, `ResourceSummary`, `ResourceDetail`, request base classes |
| `Fdw.Web.RestEndpoints` | Tier 1 CRUD + security bases | `CrudListEndpoint<T>`, `CrudGetEndpoint<TReq,TDetail>`, etc. + security bases + `EndpointLogger` |

### Per-Domain Endpoint Packages (Tier 2)

| Package | Domain | Operations |
|---------|--------|------------|
| `Services.Connections.Endpoints` | Connections | List, Get, Create, Update, Delete, TestConnection |
| `Services.Data.Endpoints` | DataStores, DataSets, Containers | Full CRUD + Introspect, Paths, Fields, Sources, ConnectionsByType |
| `Services.Pipelines.Endpoints` | Pipelines | List, GetStatus, BulkStatus |
| `Services.Scheduling.Endpoints` | Schedules | List |
| `Services.Users.Endpoints` | Users | List, Get, Create, Update, Delete, GetMe |
| `Services.Authentication.Endpoints` | Authentication | Token (login), RefreshToken, Logout, GetMe |
| `Services.Authorization.Endpoints` | Roles, Permissions, UserRoles | Full CRUD + permission matrix |
| `Services.Multitenancy.Endpoints` | Tenants | List, Get, GetCurrent, SwitchTenant |
| `Calculations.Endpoints` | Calculations | Execute, Preview, ListTypes, ListPeriodComparisons |
| `Services.Quality.Endpoints` | Quality | CreateQualityRule |
| `Services.Catalog.Endpoints` | Catalog | Search, Glossary CRUD, DataSet annotations |
| `Operations.Endpoints` | Executions, Dataflow, ConfigurationMetadata | Trigger, Cancel, Pause, Resume, Lineage, Impact |
| `Schema.Endpoints` | Schema | Discovery, import |
| `Web.Search.Endpoints` | Search | Full-text search |
| `UI.Themes.Endpoints` | Themes | List, Get, Create, Update, Delete, Default |

Consumer projects reference only the endpoint packages they need, rather than a monolithic API package.

## Tier 1: Abstract CRUD Base Classes

Located in [`src/Fdw.Web.RestEndpoints/Crud/`](../src/Fdw.Web.RestEndpoints/Crud/).

Each base class provides a complete `HandleAsync()` implementation with structured error handling. Derived classes only implement the abstract data-access methods.

### CrudListEndpoint

Two overloads: parameterless (for simple lists) and with a request type (for filtered/paginated lists).

From [`CrudListEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudListEndpoint.cs):

```csharp
public abstract class CrudListEndpoint<TSummary> : EndpointWithoutRequest<List<TSummary>>
    where TSummary : class
{
    // Abstract - you must implement:
    protected abstract string ResourceName { get; }
    protected abstract Task<IGenericResult<List<TSummary>>> LoadItems(CancellationToken ct);

    // Virtual - override to customize:
    protected virtual string ReadPolicy => $"fdw:{ResourceName}:read";
    protected virtual string Route => $"/{ResourceName}";
    protected virtual string EndpointSummary => $"List {ResourceName}";
    protected virtual string EndpointDescription => ...;
    protected virtual void ConfigureEndpoint() { }
    protected virtual async Task SendListResponse(List<TSummary> items, CancellationToken ct) { ... }
    protected virtual async Task SendErrorResponse(IGenericResult result, CancellationToken ct) { ... }
}
```

### CrudGetEndpoint

From [`CrudGetEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudGetEndpoint.cs):

```csharp
public abstract class CrudGetEndpoint<TRequest, TDetail> : Endpoint<TRequest, TDetail>
    where TRequest : notnull, new()
    where TDetail : class
{
    // Abstract - you must implement:
    protected abstract string ResourceName { get; }
    protected abstract Task<IGenericResult<TDetail?>> FindByIdentifier(TRequest request, CancellationToken ct);
    protected abstract string GetResourceIdentifier(TRequest request);

    // Virtual hooks for logging:
    protected virtual void OnBeforeGet(string identifier) { }
    protected virtual void OnNotFound(string identifier) { }
    protected virtual void OnAfterGet(string identifier) { }
}
```

The `HandleAsync()` flow: resolve logger, call `OnBeforeGet()`, call `FindByIdentifier()`, return 404 if null (calling `OnNotFound()`), otherwise return 200 (calling `OnAfterGet()`).

### CrudCreateEndpoint

From [`CrudCreateEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudCreateEndpoint.cs):

```csharp
public abstract class CrudCreateEndpoint<TCreateRequest, TDetail> : Endpoint<TCreateRequest, TDetail>
    where TCreateRequest : notnull, new()
    where TDetail : class
{
    // Abstract - you must implement:
    protected abstract string ResourceName { get; }
    protected abstract string GetResourceName(TCreateRequest request);
    protected abstract Task<IGenericResult<bool>> CheckExists(TCreateRequest request, CancellationToken ct);
    protected abstract Task<IGenericResult<TDetail>> Create(TCreateRequest request, CancellationToken ct);

    // Virtual - override for custom behavior:
    protected virtual string WritePolicy => $"fdw:{ResourceName}:write";
    protected virtual Task<IGenericResult> ValidateCreate(TCreateRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult>(GenericResult.Success());
    protected virtual async Task SendCreatedResponse(TDetail detail, CancellationToken ct) { ... }

    // Virtual hooks:
    protected virtual void OnBeforeCreate(string resourceName) { }
    protected virtual void OnAlreadyExists(string resourceName) { }
    protected virtual void OnAfterCreate(string resourceName) { }
}
```

The `HandleAsync()` flow: `OnBeforeCreate()` -> `CheckExists()` (409 if duplicate) -> `ValidateCreate()` -> `Create()` -> `OnAfterCreate()` -> 201 Created.

### CrudUpdateEndpoint

From [`CrudUpdateEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudUpdateEndpoint.cs):

```csharp
public abstract class CrudUpdateEndpoint<TUpdateRequest, TDetail> : Endpoint<TUpdateRequest, TDetail>
    where TUpdateRequest : notnull, new()
    where TDetail : class
{
    // Abstract:
    protected abstract string ResourceName { get; }
    protected abstract string GetResourceIdentifier(TUpdateRequest request);
    protected abstract Task<IGenericResult<TDetail?>> FindForUpdate(TUpdateRequest request, CancellationToken ct);
    protected abstract Task<IGenericResult<TDetail>> PerformUpdate(TUpdateRequest request, TDetail existing, CancellationToken ct);

    // Virtual:
    protected virtual HttpVerb UpdateVerb => HttpVerb.PUT;  // Or PATCH
    protected virtual Task<IGenericResult> ValidateUpdate(TUpdateRequest request, TDetail existing, CancellationToken ct) { ... }
    protected virtual void OnBeforeUpdate(string identifier) { }
    protected virtual void OnNotFound(string identifier) { }
    protected virtual void OnAfterUpdate(string identifier) { }
}
```

### CrudDeleteEndpoint

From [`CrudDeleteEndpoint.cs`](../src/Fdw.Web.RestEndpoints/Crud/CrudDeleteEndpoint.cs):

```csharp
public abstract class CrudDeleteEndpoint<TRequest> : Endpoint<TRequest, object>
    where TRequest : notnull, new()
{
    // Abstract:
    protected abstract string ResourceName { get; }
    protected abstract string GetResourceIdentifier(TRequest request);
    protected abstract Task<IGenericResult<bool>> CheckExistsForDelete(TRequest request, CancellationToken ct);
    protected abstract Task<IGenericResult> PerformDelete(TRequest request, CancellationToken ct);

    // Virtual:
    protected virtual string DeletePolicy => $"fdw:{ResourceName}:delete";
    protected virtual Task<IGenericResult> ValidateDelete(TRequest request, CancellationToken ct) { ... }
    protected virtual void OnBeforeDelete(string identifier) { }
    protected virtual void OnNotFound(string identifier) { }
    protected virtual void OnAfterDelete(string identifier) { }
}
```

The `HandleAsync()` flow: `OnBeforeDelete()` -> `CheckExistsForDelete()` (404 if not found) -> `ValidateDelete()` (409 if dependencies exist) -> `PerformDelete()` -> `OnAfterDelete()` -> 204 No Content.

### Summary: Abstract vs Virtual

| Base Class | Abstract Members | Virtual Hooks |
|------------|-----------------|---------------|
| `CrudListEndpoint<T>` | `ResourceName`, `LoadItems()` | `SendListResponse()`, `SendErrorResponse()`, `ConfigureEndpoint()` |
| `CrudGetEndpoint<TReq,T>` | `ResourceName`, `FindByIdentifier()`, `GetResourceIdentifier()` | `OnBeforeGet()`, `OnNotFound()`, `OnAfterGet()` |
| `CrudCreateEndpoint<TReq,T>` | `ResourceName`, `GetResourceName()`, `CheckExists()`, `Create()` | `ValidateCreate()`, `SendCreatedResponse()`, `OnBeforeCreate()`, `OnAlreadyExists()`, `OnAfterCreate()` |
| `CrudUpdateEndpoint<TReq,T>` | `ResourceName`, `GetResourceIdentifier()`, `FindForUpdate()`, `PerformUpdate()` | `ValidateUpdate()`, `UpdateVerb`, `OnBeforeUpdate()`, `OnNotFound()`, `OnAfterUpdate()` |
| `CrudDeleteEndpoint<TReq>` | `ResourceName`, `GetResourceIdentifier()`, `CheckExistsForDelete()`, `PerformDelete()` | `ValidateDelete()`, `OnBeforeDelete()`, `OnNotFound()`, `OnAfterDelete()` |

## Tier 2: Per-Domain Endpoint Packages

Each `.Endpoints` package contains **generic base classes** with open type parameters and **shared DTOs**. The generic type parameters represent the concrete configuration or service types that vary between implementations.

### Package Contents Pattern

Each endpoint package contains:
- `*EndpointBase.cs` - Abstract/generic base endpoint classes
- `*Dto.cs` - Shared DTO types (summary, detail)
- `*Request.cs` - Request types for each operation

### Example: Generic Base Endpoint

From [`ListConnectionsEndpointBase.cs`](../src/Fdw.Services.Connections.Endpoints/ListConnectionsEndpointBase.cs):

```csharp
public abstract class ListConnectionsEndpointBase : CrudListEndpoint<ConnectionSummaryDto>
{
    private readonly IOptionsMonitor<List<ConnectionConfiguration>> _configurations;

    protected ListConnectionsEndpointBase(IOptionsMonitor<List<ConnectionConfiguration>> configurations)
    {
        _configurations = configurations;
    }

    protected override string ResourceName => "connections";

    protected override Task<IGenericResult<List<ConnectionSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var items = _configurations.CurrentValue
            .Where(config => !string.IsNullOrWhiteSpace(config.Name) && ...)
            .Select(MapToSummary)
            .ToList();
        return Task.FromResult(GenericResult<List<ConnectionSummaryDto>>.Success(items));
    }

    protected virtual ConnectionSummaryDto MapToSummary(ConnectionConfiguration config) { ... }
}
```

### Example: Generic Base with Type Parameter

From [`GetConnectionEndpointBase.cs`](../src/Fdw.Services.Connections.Endpoints/GetConnectionEndpointBase.cs):

```csharp
public abstract class GetConnectionEndpointBase<TConfig> : CrudGetEndpoint<ConnectionNameRequest, ConnectionDetailDto>
    where TConfig : ConnectionConfiguration
{
    private readonly IOptionsMonitor<List<TConfig>> _configurations;

    protected GetConnectionEndpointBase(IOptionsMonitor<List<TConfig>> configurations)
    {
        _configurations = configurations;
    }

    protected override string ResourceName => "connections";
    protected override string GetResourceIdentifier(ConnectionNameRequest request) => request.Name;

    protected override Task<IGenericResult<ConnectionDetailDto?>> FindByIdentifier(
        ConnectionNameRequest request, CancellationToken ct) { ... }

    protected abstract ConnectionDetailDto MapToDetail(TConfig config);
}
```

The `TConfig` type parameter is left open -- consumers close it with their concrete configuration type (e.g., `MsSqlConnectionConfiguration`).

## Data Access Patterns

Different domains use different data access strategies depending on their data source:

| Pattern | Domains | How It Works |
|---------|---------|--------------|
| **IOptionsMonitor** | Connections, DataStores (Tier 2) | Reads from `IConfiguration` populated by `MsSqlConfigurationSource` at startup. Zero database round-trips at request time. |
| **IDataGateway** | DataStores, DataSets, Schedules, Themes, Quality, Catalog, FieldMappings, Pipelines (Tier 3) | Queries ConfigurationDb via `QueryCommand<T>`, `InsertCommand<T>`, `UpdateCommand<T>`, `DeleteCommand`. |
| **Service Interfaces** | Users (`IUserStore`), Roles (`IRoleStore`), Executions (`IExecutionTracker`), Tenants (`ITenantProvider`) | Domain-specific service abstractions with their own storage implementations. |
| **TypeCollections** | ConnectionTypes, Permissions, ConfigurationMetadata | In-memory lookups via `ConnectionTypes.All()`, `FdwPermissions.All()`, `ConfigurationTypes.All()` -- O(1) FrozenDictionary access. |

## Contract Base Types

Located in [`src/Fdw.Web.Endpoints/Contracts/`](../src/Fdw.Web.Endpoints/Contracts/).

These provide consistent shapes for all endpoint DTOs and requests:

| Type | Purpose | Properties |
|------|---------|------------|
| `INamedResource` | Marker interface for resources identified by name | `string Name` |
| `ResourceSummary` | Base for list-operation DTOs | `string Name` (from `INamedResource`) |
| `ResourceDetail` | Base for get/create/update response DTOs | `Guid Id`, `string Name` |
| `ResourceNameRequest` | Base for name-identified operations | `[Required] string Name` |
| `ResourceIdRequest` | Base for ID-identified operations | `[Required] Guid Id` |
| `ResourceCreateRequest` | Base for creation requests | `[Required] [StringLength(256)] string Name` |
| `ResourceUpdateRequest` | Base for update requests (nullable properties for partial updates) | `[Required] string Name` |

### Example: Domain DTOs

From [`ConnectionSummaryDto.cs`](../src/Fdw.Services.Connections.Endpoints/ConnectionSummaryDto.cs) and [`ConnectionDetailDto.cs`](../src/Fdw.Services.Connections.Endpoints/ConnectionDetailDto.cs):

```csharp
public class ConnectionSummaryDto : ResourceSummary
{
    public required string ConnectionType { get; set; }
}

public class ConnectionDetailDto : ResourceDetail
{
    public required string ServiceType { get; set; }
    public required string Server { get; set; }
    // ... domain-specific fields
}

public class ConnectionNameRequest : ResourceNameRequest { }

public class CreateConnectionRequest : ResourceCreateRequest
{
    [Required] public string ServiceType { get; set; } = string.Empty;
    [Required] public string Server { get; set; } = string.Empty;
    // ... domain-specific fields
}

public class UpdateConnectionRequest : ResourceUpdateRequest
{
    public string? Server { get; set; }  // Nullable for partial update
    public int? Port { get; set; }
    // ...
}
```

## Authorization Pattern

All CRUD base classes use a conditional compilation guard for development vs production:

```csharp
public override void Configure()
{
    Get(Route);
#if DEVELOP
    AllowAnonymous();
#else
    Policies(ReadPolicy);
#endif
}
```

Policy names follow the convention `fdw:{resource}:{action}`:

| Action | Policy Example | CRUD Base Property |
|--------|---------------|-------------------|
| Read | `connections:read` | `ReadPolicy` |
| Write | `connections:write` | `WritePolicy` |
| Delete | `connections:delete` | `DeletePolicy` |

Build with `dotnet build -c Develop` to enable `AllowAnonymous()` for local development. Production builds (`-c Release`) enforce policy-based authorization backed by database RBAC (see [12-05 Authorization](12-05-Authorization.md)).

## Security Endpoint Bases

Located in [`src/Fdw.Web.RestEndpoints/Security/`](../src/Fdw.Web.RestEndpoints/Security/).

For endpoints that don't fit the CRUD pattern but need consistent security configuration, FDW provides four security-level base classes:

| Base Class | Access Level | Authorization | Rate Limiting |
|------------|-------------|---------------|---------------|
| `PublicEndpointBase` | Anonymous | None | Standard policy |
| `AuthenticatedEndpointBase` | Authenticated users | Requires valid token | Standard policy |
| `ProtectedEndpointBase<TRequest, TResponse>` | Policy-based | `fdw:{resource}:{action}` | Standard policy |
| `AdminEndpointBase<TRequest, TResponse>` | Admin only | `fdw:admin` | Strict policy |

Each base class sets `Configure()` with the appropriate auth and rate limiting. Derived endpoints implement `HandleAsync()` with their domain logic.

`SecurityEndpointLog` (EventIds 6400-6404) provides structured logging for security endpoint operations.

## ApiEndpointLog

Located in [`src/Fdw.Schema.Endpoints/ApiEndpointLog.cs`](../src/Fdw.Schema.Endpoints/ApiEndpointLog.cs).

Generic CRUD logging shared across endpoint packages. EventId range: **4500-4530**.

| EventId | Level | Message |
|---------|-------|---------|
| 4500 | Information | `Listing {resourceName}` |
| 4501 | Information | `Listed {count} {resourceName}` |
| 4502 | Information | `Getting {resourceName} '{name}'` |
| 4503 | Warning | `{resourceName} '{name}' not found` |
| 4504 | Information | `Creating {resourceName} '{name}'` |
| 4505 | Information | `Created {resourceName} '{name}'` |
| 4506 | Warning | `{resourceName} '{name}' already exists` |
| 4507 | Information | `Updating {resourceName} '{name}'` |
| 4508 | Information | `Updated {resourceName} '{name}'` |
| 4509 | Information | `Deleting {resourceName} '{name}'` |
| 4510 | Information | `Deleted {resourceName} '{name}'` |
| 4511 | Error | `Failed to {operation} {resourceName} '{name}'` |
| 4512 | Warning | `Validation failed for {resourceName} '{name}': {reason}` |
| 4513 | Information | `Testing connection '{name}'` |
| 4514 | Information | `Connection test for '{name}' {result}` |
| 4515 | Warning | `Domain '{domainName}' is disabled via configuration` |
| 4516 | Error | `Configuration writer unavailable for {resourceName}` |

The Tier 1 `EndpointLogger` (in `Web.RestEndpoints`) provides low-level endpoint error logging (EventId 8001-8011) used by the `HandleAsync()` catch blocks.

## Consumer Package Selection

Unlike a monolithic API package, consumers reference only the endpoint packages they need:

```xml
<ItemGroup>
  <!-- Only reference the domains you use -->
  <PackageReference Include="Fdw.Services.Connections.Endpoints" />
  <PackageReference Include="Fdw.Services.Data.Endpoints" />
  <PackageReference Include="Fdw.Services.Users.Endpoints" />
  <PackageReference Include="Fdw.UI.Themes.Endpoints" />
</ItemGroup>
```

Each endpoint package transitively pulls in `Web.RestEndpoints` and `Web.Endpoints`, so you only need the domain-specific references.

## Tier 3: Consumer Closures (Thin Closure Pattern)

Consumer projects create **thin sealed classes** that close the generic type parameters from Tier 2 with concrete types. This is called the "thin closure" pattern because the consumer class is minimal -- it only supplies the concrete type and implements required abstract methods.

`Reference.Api` in the separate **reference-api** repository (`src/Reference.Api/Endpoints/`) demonstrates the pattern.

### Pattern 1: Minimal Closure (No Logic)

Example pattern from `ConnectionsEndpoint.cs` in the **reference-api** repository:

```csharp
// Closes the generic base with no additional logic
public sealed class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    public ListConnectionsEndpoint(
        IOptionsMonitor<List<ConnectionConfiguration>> configurations)
        : base(configurations) { }
}
```

### Pattern 2: Closure with Type Parameter + Mapping

The most common pattern. The consumer closes `TConfig` and implements the abstract `MapToDetail()` method with implementation-specific field mapping.

Example pattern from `ConnectionsEndpoint.cs` in the **reference-api** repository:

```csharp
// Closes TConfig with MsSqlConnectionConfiguration
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    public GetConnectionEndpoint(IOptionsMonitor<List<MsSqlConnectionConfiguration>> configurations)
        : base(configurations) { }

    protected override ConnectionDetailDto MapToDetail(MsSqlConnectionConfiguration config)
    {
        return new ConnectionDetailDto
        {
            Id = config.Id,
            Name = config.Name,
            ServiceType = config.ServiceOptionType ?? "MsSql",
            Server = config.Server,
            Port = config.Port,
            Database = config.Database,
            // ... MsSql-specific fields
        };
    }
}
```

### Pattern 3: Standalone Endpoints

For domains requiring complex data access (like DataStores with IDataGateway), consumers implement endpoints directly using `Endpoint<TRequest, TResponse>`, following the same authorization and error handling patterns.

## Internal Server Endpoints

Beyond the consumer-facing endpoint architecture above, the reference-etl and reference-scheduler expose their own class-based FastEndpoints for internal service-to-service communication. All internal servers use a consistent `api/v1` route prefix configured via `UseFastEndpoints`.

### reference-etl Endpoints

| Endpoint Class | Method | Route | Description |
|---------------|--------|-------|-------------|
| `TriggerJobEndpoint` | POST | `/api/v1/etl/trigger` | Triggers a pipeline execution, returns 202 Accepted with execution ID |
| `GetJobStatusEndpoint` | GET | `/api/v1/etl/jobs/{executionId}/status` | Returns execution status and metrics |

### reference-scheduler Endpoints

| Endpoint Class | Method | Route | Description |
|---------------|--------|-------|-------------|
| `ListSchedulesEndpoint` | GET | `/api/v1/schedules` | Returns all configured schedules |
| `GetScheduleEndpoint` | GET | `/api/v1/schedules/{name}` | Returns a specific schedule by name |
| `CreateScheduleEndpoint` | POST | `/api/v1/schedules` | Creates a new schedule (201 Created) |
| `UpdateScheduleEndpoint` | PUT | `/api/v1/schedules/{name}` | Updates an existing schedule |
| `DeleteScheduleEndpoint` | DELETE | `/api/v1/schedules/{name}` | Deletes a schedule (204 No Content) |

These internal endpoints are standalone `Endpoint<TRequest, TResponse>` classes (not derived from the three-tier CRUD base classes) because they serve domain-specific operations rather than generic resource management. They inject domain services (e.g., `IJobExecutionService`, `ISchedulerService`) via constructor and follow the same `Configure()` / `HandleAsync()` pattern as all other FastEndpoints.

Internal endpoints are protected by `InternalApiKeyMiddleware` rather than JWT authorization. See [12-03 Service Communication](12-03-Service-Communication.md) for details on inter-service authentication and the typed client packages used to call these endpoints.

## See Also

- [Customizing Endpoints](12-08-Customizing-Endpoints.md) - Step-by-step guide for creating and overriding endpoints
- [Authorization](12-05-Authorization.md) - RBAC policy system
- [Creating a Server](12-01-Creating-A-Server.md) - Hosting extensions for server startup
- [MessageLogging Overview](07-01-Overview.md) - Structured logging patterns
