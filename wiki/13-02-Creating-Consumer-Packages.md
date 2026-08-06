# Creating Consumer Packages

Consumer projects build application-specific REST APIs by creating **thin closures** over per-domain `.Endpoints` packages. Each endpoint package provides generic base classes and shared DTOs; consumers close the generic type parameters with their concrete types. This is the thin-client pattern: the framework provides all CRUD logic, authorization, error handling, and OpenAPI documentation; consumers provide only the type-specific mapping.

## What Consumers Get

| Feature | Source | What You Get |
|---------|--------|--------------|
| CRUD base classes | `Web.RestEndpoints` | Routing, authorization, error handling, OpenAPI summaries |
| Generic domain endpoints | 13 `.Endpoints` packages | Base classes with open type parameters + shared DTOs |
| Structured logging | `ApiEndpointLog` | EventIds 4500-4516 with `{resourceName}` and `{name}` parameters |
| Endpoint contracts | `Web.Endpoints` | `ResourceSummary`, `ResourceDetail`, `PaginatedRequest`, etc. |
| Cross-domain DTO contracts | `Web.Clients.Abstractions` | Interface abstractions (`IColumnSchema`, `IDataSetField`, etc.) for DTOs shared across domains |

## Package Dependency Chain

### Server-Side (API Endpoints)

```
Consumer API (e.g., Reference.Api)
  |
  +-- Fdw.Services.Connections.Endpoints  (Connection generic bases)
  |     +-- Fdw.Web.RestEndpoints          (Tier 1 CRUD bases)
  |     +-- Fdw.Web.Endpoints              (Shared contracts)
  |     +-- Fdw.Services.Connections       (Domain services)
  |
  +-- Fdw.Services.Data.Endpoints         (DataStore/DataSet bases)
  +-- Fdw.Operations.Endpoints            (Executions, dataflow, config metadata)
  +-- ... (only the domains you need -- 13 endpoint packages available)
  |
  +-- Fdw.Hosting.MsSql                   (Server startup)
  +-- Fdw.Configuration.Writers           (CQRS write-side)
  +-- Application-specific packages
```

### Client-Side (UI and API Clients)

```
Consumer UI (e.g., reference-ui)
  |
  +-- Fdw.UI.Components.Blazor            (Protocol providers)
  |     +-- Per-domain .Clients packages (transitive, 15 packages)
  |           +-- Services.Connections.Clients          (ConnectionApiClient)
  |           +-- Services.Data.Clients                 (DataStoreApiClient, DataSetApiClient)
  |           +-- Services.Pipelines.Clients            (PipelineApiClient)
  |           +-- Services.Scheduling.Clients           (ScheduleApiClient)
  |           +-- Services.Users.Clients                (UserApiClient)
  |           +-- Services.Authorization.Clients        (RoleApiClient)
  |           +-- Services.Multitenancy.Clients         (TenantApiClient)
  |           +-- Services.Quality.Clients              (QualityApiClient)
  |           +-- Services.Catalog.Clients              (CatalogApiClient)
  |           +-- Schema.Clients                        (SchemaApiClient)
  |           +-- Operations.Clients                    (DataflowApiClient, ConfigurationApiClient)
  |           +-- Web.Analytics.Clients                 (AnalyticsApiClient)
  |           +-- Web.Calculations.Clients              (CalculationApiClient)
  |           +-- Web.Search.Clients                    (SearchApiClient)
  |           +-- UI.Themes.Clients                     (ThemeApiClient)
  |           +-- Web.Clients.Abstractions              (ApiClientBase, ClientLog)
  |                 +-- Contracts/                      (IColumnSchema, IDataSetField, etc.)
  |
  +-- Fdw.UI.Blazor.MudBlazor             (or Tailwind helpers)
  +-- Application-specific rendering
```

## Creating Consumer Endpoints (Thin Closure Pattern)

### Option 1: Thin Closures Over Endpoint Packages

The primary pattern. Reference each domain package and create sealed classes that close the generic type parameters:

```xml
<ItemGroup>
  <PackageReference Include="Fdw.Services.Connections.Endpoints" />
  <PackageReference Include="Fdw.Services.Data.Endpoints" />
</ItemGroup>
```

```csharp
// Minimal closure -- no type parameter, no abstract methods
public sealed class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    public ListConnectionsEndpoint(
        IOptionsMonitor<List<ConnectionConfiguration>> configurations)
        : base(configurations) { }
}

// Closure with type parameter -- close TConfig, implement abstract mapping
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    public GetConnectionEndpoint(
        IOptionsMonitor<List<MsSqlConnectionConfiguration>> configurations)
        : base(configurations) { }

    protected override ConnectionDetailDto MapToDetail(MsSqlConnectionConfiguration config)
    {
        return new ConnectionDetailDto
        {
            Id = config.Id,
            Name = config.Name,
            ServiceType = config.ServiceOptionType ?? "MsSql",
            Server = config.Server,
            // ... implementation-specific fields
        };
    }
}
```

### Option 2: Write Custom Endpoints

For application-specific domains not covered by the framework, write endpoints directly against the CRUD base classes:

```csharp
public sealed class ListNflTeamsEndpoint : CrudListEndpoint<NflTeamSummary>
{
    private readonly IOptionsMonitor<List<NflTeamConfiguration>> _teams;

    public ListNflTeamsEndpoint(IOptionsMonitor<List<NflTeamConfiguration>> teams)
    {
        _teams = teams;
    }

    protected override string ResourceName => "nfl-teams";

    protected override Task<IGenericResult<List<NflTeamSummary>>> LoadItems(CancellationToken ct)
    {
        var items = _teams.CurrentValue
            .Select(t => new NflTeamSummary { Name = t.Name, Division = t.Division })
            .ToList();
        return Task.FromResult(GenericResult<List<NflTeamSummary>>.Success(items));
    }
}
```

## Route Differences: REST vs RPC

The endpoint base classes use **REST-style routes** by default:

| Operation | Verb | Route |
|-----------|------|-------|
| List | GET | `/{resource}` |
| Get | GET | `/{resource}/{name}` |
| Create | POST | `/{resource}` |
| Update | PUT | `/{resource}/{name}` |
| Delete | DELETE | `/{resource}/{name}` |

Consumer projects may override `Configure()` for **RPC-style routes** (useful for Blazor compatibility):

| Operation | Verb | Route |
|-----------|------|-------|
| List | POST | `/{resource}/list` |
| Get | POST | `/{resource}/get` |
| Create | POST | `/{resource}` |
| Update | POST | `/{resource}/update` |
| Delete | POST | `/{resource}/delete` |

## Example: Reference.Api Connections

Example pattern (the full `ConnectionsEndpoint.cs` lives in the separate **reference-api** repository):

```csharp
// Connections closures (all in ConnectionsEndpoint.cs):
public sealed class ListConnectionsEndpoint : ListConnectionsEndpointBase { ... }
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration> { ... }
public sealed class CreateConnectionEndpoint : CreateConnectionEndpointBase<MsSqlConnectionConfiguration> { ... }
public sealed class UpdateConnectionEndpoint : UpdateConnectionEndpointBase<MsSqlConnectionConfiguration> { ... }
public sealed class DeleteConnectionEndpoint : DeleteConnectionEndpointBase<MsSqlConnectionConfiguration> { ... }

// Other domains use one file per endpoint:
// CreateDataStoreEndpoint.cs, GetDataStoreEndpoint.cs, ListDataStoresEndpoint.cs, etc.
```

Each closure is typically 5-30 lines -- just constructor forwarding plus abstract method implementations for type-specific mapping. Closures can be organized either in a single file per domain or one file per endpoint.

## Creating Consumer UI

Consumer UI projects are rendering-only skins that consume FDW Protocol providers via the headless architecture. The Protocol providers handle all API calls, state management, error handling, and caching; the consumer UI only renders the state they provide.

### Package References

```xml
<PackageReference Include="Fdw.UI.Components.Blazor" />
<PackageReference Include="Fdw.UI.Blazor.MudBlazor" />  <!-- or your UI framework -->
<!-- Per-domain .Clients packages are transitive from UI.Components.Blazor -->
```

### Per-Domain .Clients Packages (15 total)

| Package | Primary Client | Purpose |
|---------|---------------|---------|
| `Services.Connections.Clients` | `ConnectionApiClient` | Connection CRUD |
| `Services.Data.Clients` | `DataStoreApiClient`, `DataSetApiClient` | Data store/set management |
| `Services.Pipelines.Clients` | `IPipelineClient`, `IPipelineJobClient` | Pipeline configuration, job execution |
| `Services.Scheduling.Clients` | `ScheduleApiClient` | Schedule management |
| `Services.Users.Clients` | `UserApiClient` | User management |
| `Services.Authorization.Clients` | `RoleApiClient` | RBAC permissions |
| `Services.Multitenancy.Clients` | `TenantApiClient` | Multi-tenancy |
| `Services.Quality.Clients` | `QualityApiClient` | Data quality |
| `Services.Catalog.Clients` | `CatalogApiClient` | Catalog browsing |
| `Operations.Clients` | `ConfigurationApiClient`, `DataflowApiClient`, `LineageApiClient` | Operations |
| `Schema.Clients` | `SchemaApiClient`, `TableApiClient` | Schema introspection |
| `UI.Themes.Clients` | `ThemeApiClient` | Theme management |
| `Web.Calculations.Clients` | `CalculationApiClient` | Calculations |
| `Web.Analytics.Clients` | `AnalyticsApiClient`, `PromotionApiClient` | Analytics |
| `Web.Search.Clients` | `SearchApiClient` | Global search |

All clients inherit from `ApiClientBase` in `Web.Clients.Abstractions`, which provides HTTP plumbing, authentication header injection, structured error handling, and `ClientLog` for structured logging.

### Cross-Domain Interface Contracts

When a DTO is used across domain boundaries (e.g., `IColumnSchema` shared between Schema and Data), the contract interface lives in `Web.Clients.Abstractions/Contracts/`. Each per-domain `.Clients` package implements the interface on its concrete DTO. This allows Protocol providers and consuming code to work with the interface without taking a dependency on a specific domain's package.

See [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) for the full list of contract interfaces and how Protocols work.

### Adding Consumer-Specific Features

For features not covered by the framework, create additional:
1. **API clients** in your project (inheriting from `ApiClientBase`)
2. **Protocol components** that inject your custom API clients
3. **Visual components** that wrap your Protocols with your UI framework

## See Also

- [12-07 API Endpoints](12-07-API-Endpoints.md) -- Per-domain endpoint architecture
- [12-08 Customizing Endpoints](12-08-Customizing-Endpoints.md) -- Thin closure pattern details
- [Web.RestEndpoints](../src/Fdw.Web.RestEndpoints/) -- Tier 1 CRUD base classes
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) -- Protocol architecture
- [12-01 Creating a Server](12-01-Creating-A-Server.md) -- Hosting startup
- [11-01 Management UI Overview](11-01-Management-UI-Overview.md) -- Reference UI implementations
