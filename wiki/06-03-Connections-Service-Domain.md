# Connections Service Domain

The Connections service domain provides a plugin architecture for database and API connections with factory-based creation, provider-based access, and type-safe configuration. This domain follows the standard service domain patterns described in [Creating a Service Domain](06-02-Creating-Service-Domain.md).

## Overview

The Connections domain consists of:

- **Abstractions Package** - `FractalDataWorks.Services.Connections.Abstractions`
  - `IGenericConnection` - Service interface
  - `IConnectionConfiguration` - Configuration interface
  - `ConnectionConfigurationBase` - Configuration base class
  - `IConnectionFactory` - Factory interface
  - `IConnectionProvider` - Provider interface
  - `ConnectionTypeBase` - ServiceType base class

- **Base Package** - `FractalDataWorks.Services.Connections`
  - `ConnectionTypes` - ServiceTypeCollection
  - `DefaultConnectionProvider` - Provider implementation
  - `ConnectionLog` - MessageLogging class

- **Implementation Packages**
  - `FractalDataWorks.Services.Connections.MsSql` - SQL Server connections
  - `FractalDataWorks.Services.Connections.PostgreSql` - PostgreSQL connections (future)
  - `FractalDataWorks.Services.Connections.Rest` - REST API connections (future)

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
└── Authentication/ (deprecated)
    └── MsSqlAuthenticationTypes.cs         # Old pattern - being replaced
```

### MsSqlAuthenticationProcessors TypeCollection

The `MsSqlAuthenticationProcessors` TypeCollection provides a type-safe, extensible way to handle different authentication strategies for SQL Server connections. This pattern replaces the older `MsSqlAuthenticationTypes` approach.

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

        command.Append($"User Id={context.Configuration.Authentication!.Username};");

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
            configuration.Authentication?.Type ?? "WindowsAuth");

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
        "Authentication": {
          "Type": "SqlAuth",
          "Username": "app_user",
          "SecretManagerName": "AzureKeyVault",
          "SecretKeyName": "OrdersDb-Password"
        }
      },
      {
        "Name": "ReportsDb",
        "Server": "localhost",
        "Database": "Reports",
        "Authentication": {
          "Type": "WindowsAuth"
        }
      }
    ]
  }
}
```

### Database Configuration Loading

When configurations are loaded from the database (via `MsSqlConfigurationProvider`), they are organized by **ServiceType** with array indexing to match the JSON structure above:

**Database Tables:**
```sql
-- Parent table (contains Name and discriminator)
cfg.Connection
  Id: GUID
  Name: 'OrdersDb'
  ServiceOptionType: 'MsSql'  -- Discriminator for ServiceType lookup

-- Shared PK extension table (1:1 relationship - same Id as parent)
cfg.ConnectionAuthentication
  Id: GUID  -- Both PK and FK to cfg.Connection(Id)
  Type: 'SqlAuth'
  Username: 'app_user'
  SecretManagerName: 'AzureKeyVault'
  SecretKeyName: 'OrdersDb-Password'

-- Child table (contains type-specific properties)
cfg.MsSqlConnection
  Id: GUID
  ConnectionId: GUID  -- FK to cfg.Connection
  Server: 'localhost'
  Database: 'Orders'
```

**Shared PK Extension Pattern:**

The `cfg.ConnectionAuthentication` table uses a shared primary key pattern where its `Id` column is both the primary key AND a foreign key to `cfg.Connection(Id)`. This creates a strict 1:1 relationship - each connection has at most one authentication record with the same ID.

This pattern is used because:
1. **Not all connections need authentication** - Some use Windows Integrated Security
2. **Keeps the parent table lean** - Core connection properties stay in `cfg.Connection`
3. **Authentication is a separate concern** - Can be loaded/validated independently

The `MsSqlConfigurationProvider` automatically loads shared PK extension tables (like `ConnectionAuthentication`) after loading the parent row, prefixing properties with the extension name (e.g., `Authentication:Type`, `Authentication:Username`).

**IConfiguration Keys Generated:**
```
Connections:MsSql:0:Name = "OrdersDb"
Connections:MsSql:0:Server = "localhost"
Connections:MsSql:0:Database = "Orders"
Connections:MsSql:0:Authentication:Type = "SqlAuth"
Connections:MsSql:0:Authentication:Username = "app_user"
```

**Key Structure:** `{ServiceCategory}s:{ServiceType}:{Index}:{Property}`
- `ServiceCategory`: Singular form (Connection, SecretManager, Pipeline)
- `{ServiceCategory}s`: Plural form for section prefix (Connections, SecretManagers, Pipelines)
- `ServiceType`: From `cfg.Connection.ConnectionType` field (MsSql, Rest, Http)
- `Index`: Zero-based array index (0, 1, 2...)
- `Property`: Property name from configuration class

**Why This Structure?**

This organization allows `IOptions<List<TConfiguration>>` binding to work correctly:

```csharp
// MsSqlConnectionType.Configure() binds ALL MsSql connections
services.Configure<List<MsSqlConnectionConfiguration>>(
    configuration.GetSection("Connections:MsSql"));

// At runtime, provider looks up by Name
var config = _options.Value.FirstOrDefault(c => c.Name == "OrdersDb");
```

**Configuration Flow:**

1. **Database** → Parent-child tables with Name and type-specific properties
2. **MsSqlConfigurationProvider** → Loads into IConfiguration with array-indexed keys
3. **ServiceTypeOption.Configure()** → Binds `IOptions<List<TConfig>>` by ServiceType
4. **DefaultConnectionProvider** → Looks up by Name from the bound list at runtime

## Processors vs Authentication Types

The processor pattern replaces the older `MsSqlAuthenticationTypes` approach:

| Aspect | Old Pattern (AuthenticationTypes) | New Pattern (Processors) |
|--------|-----------------------------------|--------------------------|
| **Purpose** | Type identity + connection string building | Separate validation and processing |
| **Base Class** | `MsSqlAuthenticationTypeBase` | `MsSqlAuthenticationProcessorBase` |
| **Collection** | `MsSqlAuthenticationTypes` | `MsSqlAuthenticationProcessors` |
| **Method** | `BuildConnectionString(config, password)` | `Validate(context)` + `Process(command, context)` |
| **Context** | Separate parameters | `MsSqlProcessorContext` record struct |
| **Validation** | Implicit in build method | Explicit `Validate()` method |
| **Extensibility** | Add new type class | Add new processor class |

### Migration Path

The old `MsSqlAuthenticationTypes` pattern is being phased out:

```csharp
// OLD PATTERN (deprecated)
var authType = MsSqlAuthenticationTypes.ByName(config.Authentication.Type);
var connectionString = authType.BuildConnectionString(config, password);

// NEW PATTERN (current)
var processor = MsSqlAuthenticationProcessors.ByName(config.Authentication.Type);
var builder = new StringBuilder("Server=...;Database=...;");
var context = new MsSqlProcessorContext(config, resolvedPassword);
var result = processor.Process(builder, context);
```

Benefits of the processor pattern:
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

The Connections domain follows the standard three-phase DI registration pattern:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Optional: Logger for startup diagnostics
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

// Phase 1: Configure and Register
ConnectionTypes.Configure(builder.Services, builder.Configuration, loggerFactory);
ConnectionTypes.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 2: Initialize (optional - for eager validation)
ConnectionTypes.Initialize(app.Services, loggerFactory);

app.Run();
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
