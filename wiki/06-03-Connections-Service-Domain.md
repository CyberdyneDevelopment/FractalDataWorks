# Connections Service Domain

The Connections service domain provides a plugin architecture for database and API connections with factory-based creation, provider-based access, and type-safe configuration. This domain follows the standard service domain patterns described in [Creating a Service Domain](06-02-Creating-Service-Domain.md).

## Overview

The Connections domain consists of:

- **Abstractions Package** - `Fdw.Services.Connections.Abstractions`
  - `IGenericConnection` - Service interface
  - `IConnectionConfiguration` - Configuration interface
  - `ConnectionConfigurationBase` - Configuration base class
  - `IConnectionFactory` - Factory interface
  - `IConnectionProvider` - Provider interface
  - `ConnectionTypeBase` - ServiceType base class

- **Base Package** - `Fdw.Services.Connections`
  - `ConnectionTypes` - ServiceTypeCollection
  - `DefaultConnectionProvider` - Provider implementation
  - `ConnectionLog` - MessageLogging class

- **Implementation Packages**
  - `Fdw.Services.Connections.MsSql` - SQL Server connections
  - `Fdw.Services.Connections.PostgreSql` - PostgreSQL connections (future)
  - `Fdw.Services.Connections.Rest` - REST API connections (future)

## MsSql Connection Implementation

The MsSql connection implementation demonstrates the processor pattern for extensible authentication strategies.

### Architecture

```
Services.Connections.MsSql/
├── MsSqlConnectionType.cs              # ServiceTypeOption registration
├── MsSqlConnectionFactory.cs           # Factory implementation
├── MsSqlConnectionConfiguration.cs     # Configuration class
├── Processors/
│   ├── MsSqlAuthenticationProcessors.cs    # Processor TypeCollection
│   ├── IMsSqlAuthenticationProcessor.cs    # Processor interface
│   ├── MsSqlAuthenticationProcessorBase.cs # Processor base class
│   ├── MsSqlProcessorContext.cs            # Processing context
│   ├── SqlAuthProcessor.cs                 # SQL authentication
│   ├── WindowsAuthProcessor.cs             # Windows authentication
│   ├── EntraIdAuthProcessor.cs             # Entra ID authentication
│   └── ManagedIdentityAuthProcessor.cs     # Managed Identity authentication
```

### MsSqlAuthenticationProcessors TypeCollection

The `MsSqlAuthenticationProcessors` TypeCollection provides a type-safe, extensible way to handle different authentication strategies for SQL Server connections.

#### Why Processors?

The processor pattern separates concerns:

1. **Validation** - Each processor validates its required configuration properties
2. **Processing** - Each processor adds authentication-specific parameters to the connection string
3. **Extensibility** - New authentication methods can be added by creating new processors
4. **Type Safety** - O(1) lookup by name with compile-time type checking

#### Key Components

**1. MsSqlAuthenticationProcessors TypeCollection**

```csharp
[TypeCollection(
    typeof(MsSqlAuthenticationProcessorBase),
    typeof(IMsSqlAuthenticationProcessor),
    typeof(MsSqlAuthenticationProcessors))]
public abstract partial class MsSqlAuthenticationProcessors
    : ProcessorCollectionBase<MsSqlAuthenticationProcessorBase, IMsSqlAuthenticationProcessor>
{
    // Source generator populates:
    // - ByName(string name)
    // - ById(int id)
    // - All()
    // - NotFound() - returns Empty sentinel
}
```

**2. IMsSqlAuthenticationProcessor Interface**

```csharp
public interface IMsSqlAuthenticationProcessor
    : IConnectionProcessor<StringBuilder, MsSqlProcessorContext>,
      ITypeOption<int, MsSqlAuthenticationProcessorBase>
{
}
```

The interface combines:
- `IConnectionProcessor<TCommand, TContext>` - for processor behavior
- `ITypeOption<TKey, TBase>` - for TypeCollection integration

**3. MsSqlAuthenticationProcessorBase Base Class**

```csharp
public abstract class MsSqlAuthenticationProcessorBase
    : ConnectionProcessorBase<StringBuilder, MsSqlProcessorContext, MsSqlAuthenticationProcessorBase>,
      IMsSqlAuthenticationProcessor
{
    protected MsSqlAuthenticationProcessorBase(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> requiredProperties)
        : base(name, displayName, description, requiredProperties)
    {
    }

    public override IGenericResult Validate(MsSqlProcessorContext context)
    {
        // Validates Server, Database, and authentication-specific required properties
    }

    protected virtual bool ValidateRequiredProperty(
        MsSqlConnectionConfiguration config,
        string propertyName,
        out string error)
    {
        // Validates specific configuration properties
    }
}
```

**4. MsSqlProcessorContext**

```csharp
public readonly record struct MsSqlProcessorContext(
    MsSqlConnectionConfiguration Configuration,
    string? ResolvedPassword);
```

This immutable context provides:
- `Configuration` - The connection configuration (server, database, auth settings)
- `ResolvedPassword` - Password resolved from secret manager (may be null)

#### Concrete Processors

**SQL Authentication Processor**

```csharp
[TypeOption(typeof(MsSqlAuthenticationProcessors), "SqlAuth")]
public sealed class SqlAuthProcessor : MsSqlAuthenticationProcessorBase
{
    public SqlAuthProcessor()
        : base("SqlAuth", "SQL Server Authentication",
               "Username and password authentication",
               ["Username", "SecretKeyName"])
    {
    }

    public override IGenericResult<StringBuilder> Process(
        StringBuilder command,
        MsSqlProcessorContext context)
    {
        var validationResult = Validate(context);
        if (!validationResult.IsSuccess)
        {
            return GenericResult<StringBuilder>.Failure(validationResult.CurrentMessage);
        }

        context.Configuration.AdditionalProperties.TryGetValue("Username", out var username);
        command.Append($"User Id={username};");

        if (!string.IsNullOrEmpty(context.ResolvedPassword))
        {
            command.Append($"Password={context.ResolvedPassword};");
        }

        return GenericResult<StringBuilder>.Success(command);
    }
}
```

**Windows Authentication Processor**

```csharp
[TypeOption(typeof(MsSqlAuthenticationProcessors), "WindowsAuth")]
public sealed class WindowsAuthProcessor : MsSqlAuthenticationProcessorBase
{
    public WindowsAuthProcessor()
        : base("WindowsAuth", "Windows Authentication",
               "Authenticate using Windows Integrated Security", [])
    {
    }

    public override IGenericResult<StringBuilder> Process(
        StringBuilder command,
        MsSqlProcessorContext context)
    {
        var validationResult = Validate(context);
        if (!validationResult.IsSuccess)
        {
            return GenericResult<StringBuilder>.Failure(validationResult.CurrentMessage);
        }

        command.Append("Integrated Security=True;");

        return GenericResult<StringBuilder>.Success(command);
    }
}
```

### Usage Example

The factory uses processors to build connection strings. Dependencies are injected via constructor:

```csharp
public class MsSqlConnectionFactory : IMsSqlConnectionFactory
{
    private readonly ILogger<MsSqlConnectionFactory> _logger;
    private readonly ILogger<MsSqlConnection> _connectionLogger;
    private readonly IFdwServiceProvider<ISecretManager, ISecretManagerConfiguration>? _secretManagerProvider;

    public MsSqlConnectionFactory(
        ILogger<MsSqlConnectionFactory> logger,
        ILogger<MsSqlConnection> connectionLogger,
        IFdwServiceProvider<ISecretManager, ISecretManagerConfiguration>? secretManagerProvider = null)
    {
        _logger = logger;
        _connectionLogger = connectionLogger;
        _secretManagerProvider = secretManagerProvider;
    }

    public IGenericResult<IGenericConnection> Create(MsSqlConnectionConfiguration configuration)
    {
        // 1. Resolve password from secret manager if needed
        string? resolvedPassword = null;
        if (!string.IsNullOrEmpty(configuration.Authentication?.SecretKeyName))
        {
            var secretManagerName = configuration.Authentication.SecretManagerName ?? "Default";
            var secretResult = _secretManagerProvider?.Get(secretManagerName);
            if (secretResult?.IsSuccess == true && secretResult.Value != null)
            {
                // Use secret manager to resolve password
                // ...
            }
        }

        // 2. Look up authentication processor by type name from configuration
        var processor = MsSqlAuthenticationProcessors.ByName(
            configuration.AuthenticationType ?? "WindowsAuth");

        if (processor.IsEmpty)
        {
            return GenericResult<IGenericConnection>.Failure(
                $"Unknown authentication type: {configuration.Authentication?.Type}");
        }

        // 3. Build base connection string
        var builder = new StringBuilder();
        builder.Append($"Server={configuration.Server};");
        builder.Append($"Database={configuration.Database};");

        // 4. Process authentication using the processor
        var processorContext = new MsSqlProcessorContext(configuration, resolvedPassword);
        var result = processor.Process(builder, processorContext);

        if (!result.IsSuccess)
        {
            return GenericResult<IGenericConnection>.Failure(result.CurrentMessage);
        }

        // 5. Create SqlConnection - connection string goes out of scope immediately
        var connectionString = builder.ToString();
        var sqlConnection = new SqlConnection(connectionString);

        // 6. Create MsSqlConnection with config and SqlConnection
        var connection = new MsSqlConnection(logger, configuration, sqlConnection);

        return GenericResult<IGenericConnection>.Success(connection);
    }
}
```

### Configuration Example

```json
{
  "Connections": {
    "MsSql": [
      {
        "Name": "OrdersDb",
        "Server": "localhost",
        "Database": "Orders",
        "AuthenticationType": "SqlAuth",
        "Authentication": {
          "Username": "app_user",
          "SecretManagerName": "AzureKeyVault",
          "SecretKeyName": "OrdersDb-Password"
        }
      },
      {
        "Name": "ReportsDb",
        "Server": "localhost",
        "Database": "Reports",
        "AuthenticationType": "WindowsAuth",
        "Authentication": {}
      }
    ]
  }
}
```

### Database Configuration Loading

Connection configurations are runtime rows in ConfigurationDb's `conn` schema, read through
`IConnectionConfigurationProvider` over `IConfigurationGateway`. Rows are never flattened into
`IConfiguration` keys — the provider composes the whole aggregate and callers dot-walk it.

**Database Tables:**
```sql
-- Parent table (identity-only header)
conn.Connection
  Id: GUID
  Name: 'OrdersDb'
  ServiceOptionType: 'MsSql'

-- Typed body (every field the factory reads at runtime)
conn.MsSqlConnection
  ConnectionRowId          -- FK to conn.Connection.RowId
  Server: 'localhost'
  Database: 'Orders'
  AuthenticationType: 'SqlAuth'   -- discriminator column

-- KVP child table (authentication key-value pairs)
conn.MsSqlConnectionAuthentication
  MsSqlConnectionRowId     -- FK to conn.MsSqlConnection.RowId
  Name: 'Username'         -- Property name
  Value: 'app_user'        -- Property value
```

**KVP Child Table Pattern:**

The `conn.MsSqlConnectionAuthentication` table stores authentication settings as key-value pairs
(configuration Pattern C — a `PropertyCollection`-typed child bound via the container's
`DataContainerKey` seed). The `AuthenticationType` discriminator on `conn.MsSqlConnection`
indicates which authentication type is active. The `MsSqlAuthenticationProcessors` TypeCollection
validates that all required properties are present for each type (e.g., SqlAuth requires Username
and SecretKeyName).

This pattern is used because:
1. **Different auth types need different properties** - SqlAuth needs Username/Password, WindowsAuth needs nothing, EntraId needs ClientId/TenantId
2. **Extensible without schema changes** - New auth types add processors, not columns
3. **Loaded by the configuration cascade** - the gateway reads the `PropertyCollection` key
   binding from the container seed and fills the configuration's `Authentication` dictionary

**Configuration Flow:**

1. **Database** → `conn.Connection` header + `conn.MsSqlConnection` typed body + KVP children
2. **IConfigurationGateway** → composes the aggregate (header, typed body, property-collection children)
3. **ConnectionConfigurationProvider** → `Get(name)` / `Get(id)` / `Get()` over the gateway, cached with tag-based invalidation
4. **DefaultConnectionProvider** → resolves the named configuration and hands it to the connection factory

## Authentication Processor Pattern

The `MsSqlAuthenticationProcessors` TypeCollection separates validation and processing for different authentication strategies:

```csharp
var processor = MsSqlAuthenticationProcessors.ByName(config.AuthenticationType);
var builder = new StringBuilder("Server=...;Database=...;");
var context = new MsSqlProcessorContext(config, resolvedPassword);
var result = processor.Process(builder, context);
```

**Benefits:**
1. **Explicit validation** - `Validate()` can be called separately
2. **Composable processing** - Multiple processors can be chained
3. **Testability** - Validation and processing can be tested independently
4. **Type safety** - Context is a readonly record struct (stack-allocated, immutable)

## Available Processors

| Processor Name | Display Name | Required Properties | Description |
|----------------|--------------|---------------------|-------------|
| `SqlAuth` | SQL Server Authentication | Username, SecretKeyName | Username and password authentication |
| `WindowsAuth` | Windows Authentication | None | Windows Integrated Security (Trusted Connection) |
| `EntraIdAuth` | Entra ID Authentication | ClientId, TenantId, SecretKeyName | Azure AD/Entra ID authentication |
| `ManagedIdentity` | Managed Identity | None | Azure Managed Identity authentication |

## Adding New Authentication Processors

To add a new authentication method:

1. **Create a processor class**
   ```csharp
   [TypeOption(typeof(MsSqlAuthenticationProcessors), "CustomAuth")]
   public sealed class CustomAuthProcessor : MsSqlAuthenticationProcessorBase
   {
       public CustomAuthProcessor()
           : base("CustomAuth", "Custom Authentication",
                  "Custom authentication method",
                  ["CustomProperty1", "CustomProperty2"])
       {
       }

       public override IGenericResult<StringBuilder> Process(
           StringBuilder command,
           MsSqlProcessorContext context)
       {
           var validationResult = Validate(context);
           if (!validationResult.IsSuccess)
           {
               return GenericResult<StringBuilder>.Failure(validationResult.CurrentMessage);
           }

           // Add custom authentication parameters
           command.Append("Custom Parameters Here;");

           return GenericResult<StringBuilder>.Success(command);
       }
   }
   ```

2. **Update configuration class** (if new properties needed)
   ```csharp
   public class MsSqlAuthenticationConfiguration
   {
       public string Type { get; set; } = "WindowsAuth";
       public string? Username { get; set; }
       public string? ClientId { get; set; }
       public string? TenantId { get; set; }
       public string? SecretManagerName { get; set; }
       public string? SecretKeyName { get; set; }
       // Add new properties as needed
   }
   ```

3. **Override ValidateRequiredProperty** (if custom validation needed)
   ```csharp
   protected override bool ValidateRequiredProperty(
       MsSqlConnectionConfiguration config,
       string propertyName,
       out string error)
   {
       return propertyName switch
       {
           "CustomProperty1" => ValidateCustomProperty1(config, out error),
           _ => base.ValidateRequiredProperty(config, propertyName, out error)
       };
   }
   ```

The source generator automatically discovers the new processor via the `[TypeOption]` attribute and adds it to the `MsSqlAuthenticationProcessors` collection.

## Three-Phase Registration

The Connections domain follows the standard three-phase DI registration pattern. `ConnectionTypes` is
an ordinary `[ServiceTypeCollection]`, so its three phases run inside the single
`PlatformServices.Configure`/`Register`/`Initialize` sweep alongside every other domain (see
[12-01 Creating a Server](12-01-Creating-A-Server.md)) — there is no hand-written
`ConnectionTypes.Configure(...)` call in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

var loggerFactory = builder.AddFrameworkSerilog("MyApp");

// Phase 1: Configure and Register — one sweep drives ConnectionTypes (and every other
// [ServiceTypeCollection]/[PlatformServiceProvider] domain) in dependency-safe order.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 2: Initialize (eager validation + auto schema discovery for MsSql) — same sweep.
PlatformServices.Initialize(app.Services, loggerFactory);

app.Run();
```

## Automatic Schema Discovery (MsSql)

During `ConnectionTypes`' own `Initialize()` — run for you inside the `PlatformServices.Initialize`
sweep — `MsSqlConnectionType.RegisterFactory()` automatically discovers and persists database
schemas for all configured MsSql connections. This eliminates the need for manual seed scripts to
define containers.

### What Happens

1. When the provider is resolved, `MsSqlConnectionType.RegisterFactory()` is called
2. For each configured MsSql connection (in parallel):
   - Creates the connection via the factory
   - Calls `MsSqlSchemaCommands.DiscoverSchema()` to introspect the database
   - Calls `MsSqlSchemaCommands.PersistSchema()` to write discovered metadata

### Configuration Tables Populated

| Table | Content |
|-------|---------|
| `data.DataStore` | One entry per database/connection |
| `data.DataPath` | One entry per schema |
| `data.DataContainer` | One entry per table/view |
| `data.DataContainerField` | One entry per column |

### Requirements

- `IConfigurationWriterFactory` must be registered (integrated into hosting startup)
- `DataStoreTypes` must be populated (module-init `RegisterMember`) and
  `ConfigurationGatewayDataStoreProvider`'s `Configure` must have run (for IOptionsMonitor bindings)
- Skips gracefully if `IConfigurationWriterFactory` is not available (e.g., in tests)

### Benefits

- **No manual seed scripts** - Container metadata is auto-discovered from the database
- **DataGateway resolution** - Containers (e.g., "Users", "Orders") can be resolved by name
- **Schema change detection** - Changes are detected on restart
- **Zero configuration** - Works automatically when configuration writers are registered

### Example Flow

```csharp
// One PlatformServices sweep replaces the per-domain Configure/Register/Initialize list —
// SecretManagerTypes, ConnectionTypes, and the DataStore domain's ConfigurationGatewayDataStoreProvider
// all participate automatically, in dependency-safe Group order.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Initialize - MsSql connections auto-discover schemas here, DataStore containers become available.
PlatformServices.Initialize(app.Services, loggerFactory);
```

## Runtime Usage

```csharp
public class MyService
{
    private readonly IConnectionProvider _connectionProvider;

    public MyService(IConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<IGenericResult> DoWork()
    {
        // Create connection by name from configuration
        var connectionResult = _connectionProvider.Create("OrdersDb");

        if (!connectionResult.IsSuccess)
        {
            return GenericResult.Failure(connectionResult.CurrentMessage);
        }

        var connection = connectionResult.Value;

        // Use the connection
        // ...
    }
}
```

## See Also

- [Service Domains Overview](06-01-Service-Domains-Overview.md)
- [Creating a Service Domain](06-02-Creating-Service-Domain.md)
- [TypeCollections Overview](04-01-Overview.md)
- [MessageLogging](07-01-Overview.md)
