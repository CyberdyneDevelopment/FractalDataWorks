# Domain: Services

## Purpose

The Services domain defines the **plugin architecture** for FractalDataWorks. Every extensible capability (connections, data stores, authentication, notifications, etc.) follows the ServiceTypeCollection pattern with three-phase DI registration.

## Sub-Domains

| Sub-Domain | Projects | Description |
|------------|----------|-------------|
| **Services.Core** | 5 | Base types, analyzers, execution abstractions |
| **Services.Connections** | 9 | Connection management (MsSql, PostgreSql, Http) |
| **Services.Data** | 6 | DataStore and DataSet service layer |
| **Services.Auth** | 13 | Authentication (JWT, SQL) + Authorization (RBAC) |
| **Services.SecretManagers** | 6 | Secret storage providers |
| **Services.ETL** | 8 | ETL pipelines and field mappers |
| **Services.Pipelines** | 5 | Pipeline orchestration |
| **Services.Transformations** | 8 | Data transformations |
| **Services.Quality** | 5 | Data quality checks |
| **Services.Notifications** | 5 | Notification channels |
| **Services.Multitenancy** | 5 | Tenant isolation |
| **Services.Users** | 5 | User management |
| **Services.Scheduling** | 5 | Schedule management |
| **Services.Calculations** | 2 | Calculation services |
| **Services.Catalog** | 3 | Service catalog |
| **Services.Workflows** | 2 | Workflow execution |
| **Services.Resiliency** | 4 | Rate limiting and retry |

## Key Patterns

### Three-Project Structure

Every service domain follows this structure:

```
Services.{Domain}.Abstractions/    # Interfaces, base classes (netstandard2.0)
  I{Domain}.cs
  I{Domain}Configuration.cs
  {Domain}ConfigurationBase.cs
  I{Domain}Factory.cs
  I{Domain}Provider.cs
  {Domain}TypeBase.cs

Services.{Domain}/                 # Provider, collection, registration
  {Domain}Types.cs
  Default{Domain}Provider.cs
  Logging/{Domain}Log.cs

Services.{Domain}.{Impl}/         # Concrete implementation
  {Impl}Type.cs
  {Impl}Factory.cs
  {Impl}Configuration.cs
  Logging/{Impl}Log.cs
```

### ServiceTypeCollection

The collection class wires up the plugin system:

```csharp
[ServiceTypeCollection(
    typeof({Domain}TypeBase<...>),
    typeof(I{Domain}Type),
    typeof({Domain}Types),
    GenerateProvider = true,
    ServiceInterface = typeof(I{Domain}),
    ConfigurationType = typeof({Domain}Configuration),
    ProviderType = typeof(Default{Domain}Provider),
    ProviderInterface = typeof(I{Domain}Provider),
    ServiceCategory = "{Domain}")]
public partial class {Domain}Types : ServiceTypeCollectionBase<...> { }
```

### Three-Phase DI Registration

```csharp
// Phase 1a: Configure -- bind IOptions<T> from IConfiguration
{Domain}Types.Configure(services, configuration, loggerFactory);

// Phase 1b: Register -- register factories and providers
{Domain}Types.Register(services, loggerFactory);

var app = builder.Build();

// Phase 2: Initialize -- eager resolve, validate, fail-fast
{Domain}Types.Initialize(app.Services, loggerFactory);
```

**Bootstrap order matters:** SecretManagers -> Connections -> Authentication -> remaining domains.

### Provider Pattern

Every domain has a default provider that resolves instances by name:

```csharp
public interface I{Domain}Provider
{
    Task<IGenericResult<I{Domain}>>                   Get(string name, CancellationToken ct = default);
    Task<IGenericResult<I{Domain}>>                   Get(Guid id,    CancellationToken ct = default);
    Task<IGenericResult<IReadOnlyList<I{Domain}>>>    Get(            CancellationToken ct = default);
}
```

> Providers use overloaded `Get(...)` methods — never `GetAll`, `List`, or
> `Fetch`. Providers return the root aggregate only; callers dot-walk to
> child resources.

The provider is registered as a singleton. Implementations are resolved during Initialize phase.

### Factory Pattern

Each implementation type has a factory:

```csharp
public interface I{Domain}Factory
{
    I{Domain} Create(I{Domain}Configuration configuration);
}
```

Factories are registered during the Register phase and invoked during Initialize.

### Client/Endpoint Pattern

Service domains that expose HTTP APIs follow a four-project extension:

```
Services.{Domain}.Clients.Abstractions/  # I{Domain}Client interface
Services.{Domain}.Clients/              # {Domain}HttpClient implementation
Services.{Domain}.Endpoints/            # FastEndpoints API
```

## Rules

1. **Connection type must be INVISIBLE** above the connection layer. No `is MsSqlConnection`, no type switches, no connection-type-specific defaults.
2. **Every service domain needs MessageLogging.** Use domain-specific log classes (e.g., `ConnectionLog`, `DataStoreLog`), never raw `ILogger`.
3. **NullLogger fallback is required** on every constructor accepting `ILogger<T>`: `logger ?? NullLogger<T>.Instance`.
4. **No service locator pattern.** Never store `IServiceProvider`. Inject dependencies directly.
5. **Catch, log, return.** Never throw exceptions. Return `GenericResult.Failure(DomainLog.Failed(...))`.
6. **Configuration properties use `{ get; set; }`** (not `{ get; init; }`). IOptions binding requires mutable setters.
7. **ServiceTypeOption implementers** must override `Configure()`, `Register()`, and `RegisterFactory()`.

## Reference Implementations

- **Connections:** `Services.Connections.Abstractions/` + `Services.Connections/` + `Services.Connections.MsSql/`
- **DataStores:** `Services.Data.Abstractions/` + `Services.Data/` + `Data.DataStores.SqlServer/`
- **Transformations:** `Services.Transformations.Calculation/`, `.Aggregation/`, `.Pivot/`, `.Lookup/`

## Related Domains

- **Collections** -- TypeCollection and ServiceTypeCollection base classes
- **Configuration** -- ManagedConfiguration provides database-backed IOptions for service config
- **Commands** -- DataGateway commands are the data access path for all services
- **Hosting** -- `AddFrameworkServiceTypes()` orchestrates three-phase registration
- **Web** -- Endpoint base classes for service API exposure
