# 12-08 Customizing Endpoints

Practical guide for creating consumer endpoints using the per-domain `.Endpoints` packages and the thin closure pattern.

## The Thin Closure Pattern

Each `.Endpoints` package provides generic base classes with open type parameters. Consumers create thin sealed classes that:

1. Close the generic type parameters with concrete types
2. Implement required abstract methods (typically mapping)
3. Optionally override virtual hooks for logging or customization

### Step 1: Reference the Endpoint Packages

```xml
<ItemGroup>
  <PackageReference Include="Fdw.Services.Connections.Endpoints" />
  <PackageReference Include="Fdw.Services.Data.Endpoints" />
  <!-- Add only the domains you need -->
</ItemGroup>
```

### Step 2: Create Thin Closures

**Minimal closure (no logic needed):**

```csharp
// ListConnectionsEndpointBase has no type parameters -- just inherit
public sealed class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    public ListConnectionsEndpoint(ConnectionConfigurationProvider configProvider)
        : base(configProvider) { }
}
```

**Closure with type parameter + abstract method implementation:**

```csharp
// GetConnectionEndpointBase<TConfig> requires a concrete config type
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    public GetConnectionEndpoint(
        ConnectionConfigurationProvider configProvider,
        IServiceConfigurationProvider<MsSqlConnectionConfiguration> typedProvider)
        : base(configProvider, typedProvider) { }

    // Implement the abstract mapping: parent header + typed body
    protected override ConnectionDetailDto MapToDetail(
        ConnectionConfiguration connection,
        MsSqlConnectionConfiguration? body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = connection.ServiceOptionType,
            Server = body?.Server,
            Port = body?.Port,
            Database = body?.Database,
            // ... typed-body fields from the variant-specific record
        };
    }
}
```

**Closure with create logic:**

```csharp
public sealed class CreateConnectionEndpoint : CreateConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    public CreateConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        IServiceConfigurationProvider<MsSqlConnectionConfiguration> typedProvider)
        : base(connectionProvider, typedProvider) { }

    // Build the variant-specific typed-body record from the request.
    protected override MsSqlConnectionConfiguration CreateTypedBody(
        CreateConnectionRequest request, Guid connectionId)
    {
        return new MsSqlConnectionConfiguration
        {
            ConnectionId = connectionId,
            Server = request.Server,
            Port = request.Port,
            Database = request.Database,
            // ... map request to typed body
        };
    }

    protected override ConnectionDetailDto MapToDetail(
        ConnectionConfiguration connection,
        MsSqlConnectionConfiguration typedBody,
        Guid connectionId) { ... }
}
```

### Step 3: Repeat for Each Domain

Each domain follows the same pattern. Create one sealed class per endpoint operation. Each endpoint gets its own file:

```
Reference.Api/Endpoints/
├── ConnectionsEndpoint.cs          ← List + Get + Create + Update + Delete closures
├── CreateDataStoreEndpoint.cs      ← Create DataStore closure
├── DeleteDataStoreEndpoint.cs      ← Delete DataStore closure
├── GetDataStoreEndpoint.cs         ← Get DataStore closure
├── ListDataStoresEndpoint.cs       ← List DataStores closure
├── UpdateDataStoreEndpoint.cs      ← Update DataStore closure
├── CreateUserEndpoint.cs           ← Create User closure
├── GetUserEndpoint.cs              ← Get User closure
├── ListUsersEndpoint.cs            ← List Users closure
└── ...                             ← ~70 endpoint files total
```

## Overriding Virtual Hooks

### Adding Logging

Override the virtual `On*()` hooks to add domain-specific structured logging:

```csharp
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    private readonly ILogger<GetConnectionEndpoint> _logger;

    public GetConnectionEndpoint(
        ConnectionConfigurationProvider configProvider,
        IServiceConfigurationProvider<MsSqlConnectionConfiguration> typedProvider,
        ILogger<GetConnectionEndpoint> logger)
        : base(configProvider, typedProvider)
    {
        _logger = logger;
    }

    protected override void OnBeforeGet(string identifier)
        => ConnectionLog.GettingConnection(_logger, identifier);

    protected override void OnNotFound(string identifier)
        => ConnectionLog.ConnectionNotFound(_logger, identifier);

    protected override ConnectionDetailDto MapToDetail(
        ConnectionConfiguration connection, MsSqlConnectionConfiguration? body) { ... }
}
```

### Customizing Routes

The base classes use RESTful verbs. Override `Configure()` to change routes:

```csharp
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    public override void Configure()
    {
        Post("/connections/get");  // POST instead of GET
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
    }
    // ...
}
```

When overriding `Configure()`, you must re-apply the authorization guard because the base `Configure()` is fully replaced.

## Adding Custom Action Endpoints

Not all endpoints fit the CRUD pattern. Custom actions inherit directly from FastEndpoints `Endpoint<TRequest, TResponse>`.

### Example: TestConnection

From [`TestConnectionEndpoint.cs`](../src/Fdw.Services.Connections.Endpoints/TestConnectionEndpoint.cs):

```csharp
public class TestConnectionEndpoint : Endpoint<TestConnectionRequest, TestConnectionResponse>
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ILogger<TestConnectionEndpoint> _logger;

    public TestConnectionEndpoint(
        IConnectionProvider connectionProvider,
        ILogger<TestConnectionEndpoint> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/connections/{Name}/test");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        Summary(s =>
        {
            s.Summary = "Test a connection";
            s.Description = "Tests connectivity to a configured connection by name.";
        });
    }

    public override async Task HandleAsync(TestConnectionRequest req, CancellationToken ct)
    {
        var result = _connectionProvider.Get(req.Name);

        if (!result.IsSuccess)
        {
            await Send.OkAsync(new TestConnectionResponse
            {
                Name = req.Name,
                Success = false,
                Message = result.CurrentMessage ?? "Unknown error"
            }, ct);
            return;
        }

        await Send.OkAsync(new TestConnectionResponse
        {
            Name = req.Name,
            Success = true,
            Message = "Connection successful"
        }, ct);
    }
}
```

### Common Custom Action Patterns

| Domain | Action | Route | Verb |
|--------|--------|-------|------|
| Connections | Test connectivity | `/connections/{Name}/test` | POST |
| DataStores | Introspect schema | `/datastores/{Name}/introspect` | POST |
| Executions | Trigger workflow | `/executions/trigger` | POST |
| Executions | Cancel execution | `/executions/{Id}/cancel` | POST |
| Executions | Pause execution | `/executions/{Id}/pause` | POST |
| Executions | Resume execution | `/executions/{Id}/resume` | POST |
| Schedules | Toggle schedule | `/schedules/{Name}/toggle` | POST |
| Themes | Set default | `/themes/{Name}/default` | POST |

## Adding Domain-Specific Logging

### Step 1: Create a MessageLogging Class

In your consumer project (e.g., `Reference.Api/Logging/`):

```csharp
public static partial class ConnectionLog
{
    [MessageLogging(EventId = 5200, Level = LogLevel.Information,
        Message = "Getting connection '{name}'")]
    public static partial IGenericMessage GettingConnection(ILogger logger, string name);

    [MessageLogging(EventId = 5201, Level = LogLevel.Warning,
        Message = "Connection '{name}' not found")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string name);
}
```

### Step 2: Wire Into Virtual Hooks

```csharp
protected override void OnBeforeGet(string identifier)
    => ConnectionLog.GettingConnection(_logger, identifier);

protected override void OnNotFound(string identifier)
    => ConnectionLog.ConnectionNotFound(_logger, identifier);
```

## Package Reference Setup

### Development (ProjectReference)

While developing alongside FractalDataWorks:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Fdw.Services.Connections.Endpoints\..." />
  <ProjectReference Include="..\..\src\Fdw.Services.Data.Endpoints\..." />
</ItemGroup>
```

### Production (PackageReference)

When FractalDataWorks packages are published to NuGet:

```xml
<ItemGroup>
  <PackageReference Include="Fdw.Services.Connections.Endpoints" />
  <PackageReference Include="Fdw.Services.Data.Endpoints" />
  <!-- Each package transitively includes Web.RestEndpoints and Web.Endpoints -->
</ItemGroup>
```

### Minimal Reference (Tier 1 Only)

If you only want the CRUD base classes without any domain-specific logic:

```xml
<ItemGroup>
  <PackageReference Include="Fdw.Web.RestEndpoints" />
  <PackageReference Include="Fdw.Web.Endpoints" />
</ItemGroup>
```

## Checklist: Creating Consumer Endpoints

1. Add `PackageReference` for each domain endpoint package you need
2. For each domain, create thin sealed closures inheriting the `*EndpointBase` classes
3. Close generic type parameters with your concrete configuration types
4. Implement abstract mapping methods (`MapToDetail()`, `CreateConfiguration()`, etc.)
5. Optionally override `On*()` hooks for domain-specific logging
6. Optionally override `Configure()` for custom routes or authorization

## Internal Server Endpoints (reference-etl / reference-scheduler)

The reference-etl and reference-scheduler use standalone FastEndpoints that inject domain services directly, without the three-tier CRUD base classes. This pattern is appropriate for internal service endpoints with domain-specific behavior.

### Pattern: Injecting Domain Services

From `Reference.Etl.Server/Endpoints/TriggerJobEndpoint.cs`:

```csharp
public sealed class TriggerJobEndpoint : Endpoint<TriggerJobRequest, TriggerJobResponse>
{
    private readonly IJobExecutionService _jobService;

    public TriggerJobEndpoint(IJobExecutionService jobService)
    {
        _jobService = jobService;
    }

    public override void Configure()
    {
        Post("etl/trigger");
        AllowAnonymous();  // Protected by InternalApiKeyMiddleware instead
        Summary(s =>
        {
            s.Summary = "Trigger an ETL job";
            s.Description = "Triggers a pipeline execution and returns the execution ID.";
        });
    }

    public override async Task HandleAsync(TriggerJobRequest req, CancellationToken ct)
    {
        var result = await _jobService.TriggerJob(
            req.PipelineName, req.TriggerSource ?? "Api", req.ScheduleName, ct)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AddError(result.CurrentMessage ?? "Failed to trigger job");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(new TriggerJobResponse
        {
            ExecutionId = result.Value,
            Status = "Triggered"
        }, 202, ct).ConfigureAwait(false);
    }
}
```

Key differences from consumer CRUD endpoints:

| Aspect | Consumer CRUD Endpoints | Internal Server Endpoints |
|--------|------------------------|--------------------------|
| Base class | `CrudListEndpoint<T>`, `CrudGetEndpoint<TReq,T>`, etc. | `Endpoint<TRequest, TResponse>` directly |
| Auth | `Policies("resource:action")` or `AllowAnonymous()` in DEVELOP | `AllowAnonymous()` (protected by `InternalApiKeyMiddleware`) |
| Route prefix | `api/v1` (consumer) | `api/v1` (same prefix, configured in `UseFastEndpoints`) |
| Dependencies | `ConnectionConfigurationProvider`, `IServiceConfigurationProvider<T>` | Domain services (`IJobExecutionService`, `ISchedulerService`) |
| Response | Standardized CRUD responses | Domain-specific response types |

### Route Configuration

All three servers (reference-api, reference-etl, reference-scheduler) configure FastEndpoints with the same `api/v1` prefix:

```csharp
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api/v1";
});
```

Endpoint routes in `Configure()` are relative to this prefix. For example, `Post("etl/trigger")` becomes `POST /api/v1/etl/trigger`.

## See Also

- [API Endpoints](12-07-API-Endpoints.md) - Architecture overview
- [Authorization](12-05-Authorization.md) - RBAC policy system
- [MessageLogging Attribute](07-02-MessageLogging-Attribute.md) - Creating log methods
- [Logger Classes](07-03-Logger-Classes.md) - Organizing log methods into classes
- [ResultCodes](07-06-ResultCodes.md) - Structured error codes for validation
