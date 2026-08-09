# Creating a Service Domain

This guide walks through creating a complete service domain from scratch. Use MsSql Connection as the reference implementation.

## Choosing a Schema for New Configuration Types

Pick the ConfigurationDb schema that matches your domain — there is one schema per service
category (`conn`, `data`, `auth`, `pipe`, `sched`, `notify`, `transform`, `workflow`,
`agent`, `audit`, `calc`, `quality`, `sec`, `settings`, `authz`, `usr`, `tenant`,
`catalog`). All `[ManagedConfiguration]` records are writable through their domain's
`IServiceConfigurationWriter<T> (or IDynamicConfigurationWriter)`.

ServiceTypeOption metadata (e.g. `OdbcConnectionType`) is registered at assembly load via
the `Registration.SourceGenerators` module initialiser — it doesn't need a database row.
ServiceConfiguration **instances** (e.g. `OdbcConnectionConfiguration`) live as runtime
rows in the domain schema and are written through `IServiceConfigurationWriter<T> (or IDynamicConfigurationWriter)`.

## Project Structure

A service domain consists of three project types:

```
src/
├── Fdw.Services.{Domain}.Abstractions/    # Interfaces, base classes
├── Fdw.Services.{Domain}/                  # Provider, collection, registration
└── Fdw.Services.{Domain}.{Implementation}/ # Concrete implementations
```

### Where Each Component Goes

| Component | Project | Example |
|-----------|---------|---------|
| `I{Domain}` interface | `.Abstractions` | `IGenericConnection` |
| `{Domain}Base` abstract class | `.Abstractions` | `ConnectionTypeBase` |
| `I{Domain}Factory` interface | `.Abstractions` | `IConnectionFactory` |
| `I{Domain}Configuration` interface | `.Abstractions` | `IConnectionConfiguration` |
| `{Domain}ConfigurationBase` class | `.Abstractions` | `ConnectionConfigurationBase` |
| `{Domain}Types` collection | Base package | `ConnectionTypes` |
| `I{Domain}Provider` interface | `.Abstractions` | `IConnectionProvider` |
| `Default{Domain}Provider` | Base package | `DefaultConnectionProvider` |
| `{Domain}Log` MessageLogging | Base package | `ConnectionLog` |
| `{Name}Type` ServiceTypeOption | `.{Implementation}` | `MsSqlConnectionType` |
| `{Name}Factory` | `.{Implementation}` | `MsSqlConnectionFactory` |
| `{Name}Configuration` | `.{Implementation}` | `MsSqlConnectionConfiguration` |
| `{Name}Log` MessageLogging | `.{Implementation}` | `MsSqlConnectionLog` |

## Step 1: Abstractions Package

### 1.1 Service Interface

```csharp
// Services.{Domain}.Abstractions/I{Domain}.cs
namespace Fdw.Services.{Domain}.Abstractions;

public interface I{Domain} : IGenericService
{
    string Name { get; }
    // Domain-specific members
}
```

### 1.2 Configuration Interface and Base

```csharp
// Services.{Domain}.Abstractions/I{Domain}Configuration.cs
public interface I{Domain}Configuration : IGenericConfiguration
{
    string Name { get; }
    string {Domain}Type { get; }
    // Common configuration properties
}

// Services.{Domain}.Abstractions/{Domain}ConfigurationBase.cs
public abstract class {Domain}ConfigurationBase<TConfiguration>
    : I{Domain}Configuration
    where TConfiguration : {Domain}ConfigurationBase<TConfiguration>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public abstract string {Domain}Type { get; }
    public abstract string SectionName { get; }
    // Common properties with defaults
}
```

### 1.3 Factory Interface

```csharp
// Services.{Domain}.Abstractions/I{Domain}Factory.cs
public interface I{Domain}Factory<TConfiguration>
    : IServiceFactory<I{Domain}, TConfiguration>
    where TConfiguration : I{Domain}Configuration
{
    Task<IGenericResult<I{Domain}>> Create{Domain}(TConfiguration configuration);
}
```

### 1.4 Provider Interface

```csharp
// Services.{Domain}.Abstractions/I{Domain}Provider.cs
// Provider interface extends the generic service provider pattern
public interface I{Domain}Provider
    : IFdwServiceProvider<I{Domain}, I{Domain}Configuration>
{
    // Inherits from IFdwServiceProvider<TService, TConfiguration>:
    // (marker interface with configuration type constraint)
    //
    // Inherits from IFdwServiceProvider<TService>:
    // IGenericResult<I{Domain}> Get(string name);
    // IGenericResult<I{Domain}> Get(Guid id);
    //
    // Inherits from IFdwServiceProvider:
    // IGenericResult<T> Get<T>(string name) where T : IGenericService;
    // IGenericResult<T> Get<T>(Guid id) where T : IGenericService;
    //
    // Note: Register methods are on DefaultServiceProvider, not the interface.
    // Providers are for getting services; registration is an implementation detail.
}
```

### 1.5 ServiceType Base Class

```csharp
// Services.{Domain}.Abstractions/{Domain}TypeBase.cs
public abstract class {Domain}TypeBase<TService, TFactory, TConfiguration>
    : ServiceTypeBase<TService, TFactory, TConfiguration, Default{Domain}Provider>
    where TService : class, I{Domain}
    where TConfiguration : class, I{Domain}Configuration
    where TFactory : class, I{Domain}Factory<TConfiguration>
{
    protected {Domain}TypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category)
    {
    }
}
```

## Step 2: Base Package

### 2.1 ServiceTypeCollection

```csharp
// Services.{Domain}/{Domain}Types.cs
[ServiceTypeCollection(
    typeof({Domain}TypeBase<I{Domain}, I{Domain}Factory<I{Domain}Configuration>, I{Domain}Configuration>),
    typeof(I{Domain}Type),
    typeof({Domain}Types),
    GenerateProvider = true,
    ServiceInterface = typeof(I{Domain}),
    ConfigurationInterface = typeof(I{Domain}Configuration),
    ProviderType = typeof(Default{Domain}Provider),
    ProviderInterface = typeof(I{Domain}Provider))]
public partial class {Domain}Types : ServiceTypeCollectionBase<
    {Domain}TypeBase<I{Domain}, I{Domain}Factory<I{Domain}Configuration>, I{Domain}Configuration>,
    I{Domain}Type<I{Domain}, I{Domain}Configuration, I{Domain}Factory<I{Domain}Configuration>>>
{
    // Configure(), Register(), Initialize() are source-generated
}
```

### 2.2 Default Provider

```csharp
// Services.{Domain}/Default{Domain}Provider.cs
public sealed class Default{Domain}Provider
    : DefaultServiceProvider<I{Domain}, I{Domain}Configuration, IServiceFactory<I{Domain}>, IServiceConfigurationProvider<I{Domain}Configuration>>,
      I{Domain}Provider
{
    public Default{Domain}Provider(ILogger<Default{Domain}Provider> logger)
        : base(logger)
    {
    }
}
```

The `DefaultServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>` base class provides:
- `Register(string serviceOptionType, IServiceFactory<TService> factory)`
- `Register(string serviceOptionType, IServiceConfigurationProvider<TConfiguration> configurationProvider)`
- `Get(string name)` / `Get(Guid id)` - looks up configuration, finds factory, creates service
- `Get<T>(string name)` / `Get<T>(Guid id)` - typed variants

### 2.3 MessageLogging Class

```csharp
// Services.{Domain}/Logging/{Domain}Log.cs
public static partial class {Domain}Log
{
    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Information,
        Message = "[{name}] Creating {domain}")]
    public static partial IGenericMessage Creating{Domain}(
        ILogger logger,
        string name);

    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Error,
        Message = "[{name}] Failed to create {domain}: {error}")]
    public static partial IGenericMessage {Domain}CreationFailed(
        ILogger logger,
        string name,
        string error);

    // Add methods for all domain operations and error conditions
}
```

### 2.5 Project References (.csproj)

```xml
<!-- Services.{Domain}/Fdw.Services.{Domain}.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Fdw.Services.{Domain}.Abstractions\..." />
  <ProjectReference Include="..\Fdw.Services.Abstractions\..." />
  <ProjectReference Include="..\Fdw.Collections.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
  <ProjectReference Include="..\Fdw.Registration.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\Fdw.MessageLogging.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
</ItemGroup>

<!-- Embed Registration generator for transitive NuGet flow -->
<ItemGroup>
  <None Include="..\Fdw.Registration.SourceGenerators\bin\$(Configuration)\netstandard2.0\Fdw.Registration.SourceGenerators.dll"
        Pack="true"
        PackagePath="analyzers/dotnet/cs"
        Visible="false"
        Condition="Exists('...')" />
</ItemGroup>
```

## Step 3: Implementation Package

### 3.1 ServiceTypeOption

```csharp
// Services.{Domain}.{Implementation}/{Implementation}Type.cs
[ServiceTypeOption(typeof({Domain}Types), "{Implementation}")]
public sealed class {Implementation}Type
    : {Domain}TypeBase<I{Domain}, I{Implementation}Factory, {Implementation}Configuration>
{
    public {Implementation}Type() : base(
        name: "{Implementation}",
        sectionName: "{Implementation}",
        displayName: "{Display Name}",
        description: "Description of this implementation",
        category: "CategoryName")
    {
    }

    public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
    {
        // Factories are singleton (stateless, thread-safe).
        services.AddSingleton<I{Implementation}Factory, {Implementation}Factory>();

        // Typed config provider is singleton (reads via IConfigurationGateway; caching lives in
        // the built-in gateway layer backed by the singleton DataGatewayResultCache, not in the provider).
        services.AddSingleton(sp => new DefaultConfigurationProvider<{Implementation}Configuration>(
            sp.GetRequiredService<IOptionsMonitor<List<{Implementation}Configuration>>>(),
            sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger<DefaultConfigurationProvider<{Implementation}Configuration>>(),
            sp.GetRequiredService<Lazy<IDataGateway>>(),
            ConfigurationTypes.GetByServiceType("{Domain}", "{Implementation}")!,
            "ConfigurationDb"));

        return services;
    }

    public override void RegisterFactory(
        Default{Domain}Provider provider,
        IServiceProvider services)
    {
        // Resolve factory and register with domain provider
        var factory = services.GetRequiredService<I{Implementation}Factory>();
        provider.Register(Name, factory);

        // Resolve typed config provider from DI (already wired with Lazy gateway)
        var configProvider = services.GetRequiredService<DefaultConfigurationProvider<{Implementation}Configuration>>();
        provider.Register(Name, configProvider);

        // Register in domain provider's typed lookup dictionary
        var domainProvider = services.GetService<{Domain}ConfigurationProvider>();
        domainProvider?.Register(Name, configProvider);
    }

    public override void Configure(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<List<{Implementation}Configuration>>(
            configuration.GetSection("{Domain}:{Implementation}"));
    }
}
```

### 3.2 Factory

Factories inject their dependencies directly via constructor. They're registered as singletons (stateless, thread-safe). Domain providers are registered **Scoped** by default (the `ProviderLifetime` named arg on `[ServiceTypeCollection]` controls this); factories wired inside a scoped provider resolver are resolved per request scope, not once at startup from root.

```csharp
// Services.{Domain}.{Implementation}/{Implementation}Factory.cs
public sealed class {Implementation}Factory : I{Implementation}Factory
{
    private readonly ILogger<{Implementation}Factory> _logger;
    private readonly ILogger<{Implementation}> _instanceLogger;
    private readonly IFdwServiceProvider<ISecretManager, ISecretManagerConfiguration>? _secretManagerProvider;

    public {Implementation}Factory(
        ILogger<{Implementation}Factory> logger,
        ILogger<{Implementation}> instanceLogger,
        IFdwServiceProvider<ISecretManager, ISecretManagerConfiguration>? secretManagerProvider = null)
    {
        _logger = logger;
        _instanceLogger = instanceLogger;
        _secretManagerProvider = secretManagerProvider;
    }

    public IGenericResult<I{Domain}> Create({Implementation}Configuration configuration)
    {
        try
        {
            {Implementation}Log.Creating{Domain}(_logger, configuration.Name);

            // Resolve secrets if needed
            string? secret = null;
            if (!string.IsNullOrEmpty(configuration.SecretManagerName))
            {
                var secretResult = _secretManagerProvider?
                    .Get(configuration.SecretManagerName);
                if (secretResult?.IsSuccess == true && secretResult.Value != null)
                {
                    // Use secret manager to retrieve secret
                    // ...
                }
            }

            var instance = new {Implementation}(configuration, _instanceLogger, secret);

            {Implementation}Log.{Domain}Created(_logger, configuration.Name);
            return GenericResult<I{Domain}>.Success(instance);
        }
        catch (Exception ex)
        {
            return GenericResult<I{Domain}>.Failure(
                {Implementation}Log.{Domain}CreationFailed(
                    _logger, configuration.Name, ex.Message));
        }
    }
}
```

### 3.3 Configuration

```csharp
// Services.{Domain}.{Implementation}/{Implementation}Configuration.cs
// The attribute carries only the ServiceCategory/ServiceType discriminators.
// Schema, table name, and parent relationships come from naming conventions /
// the IDataNode object model — not from the attribute.
[ManagedConfiguration(
    ServiceCategory = "{Domain}",
    ServiceType = "{Implementation}")]
public partial class {Implementation}Configuration
    : {Domain}ConfigurationBase<{Implementation}Configuration>
{
    public override string {Domain}Type => "{Implementation}";
    public override string SectionName =>
        string.IsNullOrEmpty(Name) ? "{Domain}" : $"{Domain}:{Name}";

    // Implementation-specific properties
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1234;
    // ...
}
```

### 3.4 MessageLogging

```csharp
// Services.{Domain}.{Implementation}/Logging/{Implementation}Log.cs
public static partial class {Implementation}Log
{
    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Information,
        Message = "[{name}] Creating {implementation} {domain}")]
    public static partial IGenericMessage Creating{Domain}(
        ILogger logger,
        string name);

    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Information,
        Message = "[{name}] {Implementation} {domain} created successfully")]
    public static partial IGenericMessage {Domain}Created(
        ILogger logger,
        string name);

    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Error,
        Message = "[{name}] Failed to create {implementation} {domain}: {error}")]
    public static partial IGenericMessage {Domain}CreationFailed(
        ILogger logger,
        string name,
        string error);
}
```

### 3.5 Project References (.csproj)

```xml
<!-- Services.{Domain}.{Implementation}/Fdw.Services.{Domain}.{Implementation}.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Fdw.Services.{Domain}\..." />
  <ProjectReference Include="..\Fdw.Registration.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\Fdw.MessageLogging.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
  <ProjectReference Include="..\Fdw.Configuration.SourceGenerators\..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
</ItemGroup>
```

## Step 4: Generated Module Initializer

The `Registration.SourceGenerators` package automatically generates a module initializer in each implementation package:

```csharp
// Generated: ServiceTypeOptionModuleInitializer.g.cs
[ModuleInitializer]
internal static void Initialize()
{
    {Domain}Types.RegisterMember(new {Implementation}Type());
}
```

This runs before `Main()`, ensuring the ServiceTypeOption is registered with the collection before `{Domain}Types.Configure()` or `{Domain}Types.Register()` is called.

## Checklist

### Abstractions Package
- [ ] `I{Domain}` service interface
- [ ] `I{Domain}Configuration` and `{Domain}ConfigurationBase`
- [ ] `I{Domain}Factory<TConfiguration>`
- [ ] `I{Domain}Provider`
- [ ] `{Domain}TypeBase` abstract class
- [ ] `I{Domain}Type` interface (if needed beyond base)

### Base Package
- [ ] `{Domain}Types` with `[ServiceTypeCollection]`
- [ ] `Default{Domain}Provider` implementation
- [ ] `{Domain}Log` MessageLogging class
- [ ] Source generator references in .csproj
- [ ] Embedded Registration generator for NuGet

### Implementation Package
- [ ] `{Implementation}Type` with `[ServiceTypeOption]`
- [ ] `{Implementation}Factory` with MessageLogging
- [ ] `{Implementation}Configuration` with `[ManagedConfiguration]`
- [ ] `{Implementation}Log` MessageLogging class
- [ ] Source generator references in .csproj

## Step 5: Database-Backed Configuration

For service domains that load configuration from SQL Server (via `MsSqlConfigurationSource`), additional setup is required beyond appsettings.json binding.

### How MsSqlConfigurationSource Works

The `MsSqlConfigurationSource` populates `IConfiguration` from database tables:

1. **Queries** `ConfigurationTypes.GetByServiceCategory(serviceCategory)` to find configuration types
2. **Filters** to child types (those with `ParentTableName` set)
3. **Queries** the child table, then joins to parent table via FK
4. **Gets Name** from the parent row
5. **Flattens** properties into `IConfiguration` as `{SectionPrefix}:{Name}:{Property}`
6. **Loads** optional key-value child tables for nested properties

### Required Database Schema Pattern

Each service domain requires a **parent-child table pattern**:

```
cfg.{Domain}              (parent - contains Name and audit columns)
    ├── Id (PK)
    ├── Name (unique)
    ├── Description
    ├── IsCurrent, IsDeleted, CreateDate, CreateBy, etc.

cfg.{Implementation}{Domain}   (child - contains type-specific properties)
    ├── Id (PK)
    ├── {Domain}Id (FK to parent)
    ├── Type-specific properties...
    ├── IsCurrent, IsDeleted, CreateDate, CreateBy, etc.

cfg.{Implementation}Authentication  (optional key-value table for nested objects)
    ├── Id (PK)
    ├── ConfigurationId (FK to child)
    ├── Name
    ├── Value
```

### Base Configuration Class (Parent Table)

The base class generates the parent table and must have `[ManagedConfiguration]`:

```csharp
// Services.{Domain}/{Domain}ConfigurationBase.cs
[ManagedConfiguration(
    ServiceCategory = "{Domain}")]    // Discriminator for the IOptions binding path
public abstract partial class {Domain}ConfigurationBase : I{Domain}Configuration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public abstract string {Domain}Type { get; }
    public abstract string SectionName { get; }
    public string? Description { get; set; }
}
```

### Child Configuration Class (Child Table)

Each implementation configuration class must reference its parent:

```csharp
// Services.{Domain}.{Implementation}/{Implementation}Configuration.cs
[ManagedConfiguration(
    ServiceCategory = "{Domain}",              // MUST match parent
    ServiceType = "{Implementation}")]         // Discriminator value
public sealed partial class {Implementation}Configuration
    : {Domain}ConfigurationBase
{
    public override string {Domain}Type => "{Implementation}";

    // Implementation-specific properties
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1234;
}
```

### Configuration Key Structure

When configurations are loaded from the database via `MsSqlConfigurationProvider`, they are organized by **ServiceType** with array indexing to enable `IOptions<List<TConfiguration>>` binding:

**Key Structure:** `{ServiceCategory}s:{ServiceType}:{Index}:{Property}`

| ServiceCategory | ConfigurationSectionPrefix | IConfiguration Key Pattern |
|-----------------|---------------------------|---------------------------|
| Connection | Connections | `Connections:MsSql:0:Name`, `Connections:MsSql:0:Server` |
| SecretManager | SecretManagers | `SecretManagers:EnvironmentVariable:0:Name`, `SecretManagers:EnvironmentVariable:0:Prefix` |
| Pipeline | Pipelines | `Pipelines:BatchCopy:0:Name`, `Pipelines:BatchCopy:0:BatchSize` |
| Schedule | Schedules | `Schedules:Cron:0:Name`, `Schedules:Cron:0:Expression` |

**Components:**
- **ServiceCategory**: Singular form (Connection, SecretManager, Pipeline, Schedule)
- **ConfigurationSectionPrefix**: Plural form (Connections, SecretManagers, Pipelines, Schedules)
- **ServiceType**: From database discriminator field (MsSql, EnvironmentVariable, BatchCopy, Cron)
- **Index**: Zero-based array index (0, 1, 2...) for multiple configurations of same type
- **Property**: Property name from configuration class

**Why This Structure?**

This organization allows `IOptions<List<TConfiguration>>` binding to work correctly, grouping all configurations of the same type together so the provider can look them up by Name at runtime.

### Configuration Binding in ServiceTypeOption

The `Configure()` method in each ServiceTypeOption binds `IOptions<List<TConfiguration>>` from the section organized by ServiceType:

```csharp
public override void Configure(
    IServiceCollection services,
    IConfiguration configuration)
{
    // Bind ALL configurations of this type from the array-indexed section
    services.Configure<List<{Implementation}Configuration>>(
        configuration.GetSection("{Domain}s:{Implementation}"));
}
```

**Example from MsSqlConnectionType:**
```csharp
public override void Configure(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<List<MsSqlConnectionConfiguration>>(
        configuration.GetSection("Connections:MsSql"));
}
```

This binds all MsSql connections from keys like:
- `Connections:MsSql:0:Name` = "NflStats"
- `Connections:MsSql:0:Server` = "localhost"
- `Connections:MsSql:1:Name` = "OrdersDb"
- `Connections:MsSql:1:Server` = "remote-server"

At runtime, the DefaultConnectionProvider looks up configurations by Name from the bound `IOptions<List<MsSqlConnectionConfiguration>>`:

```csharp
public IGenericResult<IGenericConnection> Create(string name)
{
    // Get all configurations of the requested type
    var configs = _options.Value; // List<MsSqlConnectionConfiguration>

    // Find by name
    var config = configs.FirstOrDefault(c => c.Name == name);
    if (config == null)
        return GenericResult<IGenericConnection>.Failure($"Connection not found: {name}");

    // Get factory and create instance
    var factory = _factories[config.ConnectionType];
    return factory.CreateConnection(config);
}
```

**Important:** The plural form (Connections, SecretManagers, etc.) MUST be used in the section path to match the keys generated by `MsSqlConfigurationProvider`.

### Database Seeding Pattern

When seeding configurations, insert into BOTH parent and child tables:

```sql
-- 1. Insert parent record (contains Name)
DECLARE @ParentId UNIQUEIDENTIFIER = NEWID();
INSERT INTO cfg.{Domain} (Id, Name, Description)
VALUES (@ParentId, 'ConfigName', 'Description');

-- 2. Insert child record (FK to parent, type-specific props)
DECLARE @ChildId UNIQUEIDENTIFIER = NEWID();
INSERT INTO cfg.{Implementation}{Domain} (
    Id, {Domain}Id, Server, Port, ...
)
VALUES (
    @ChildId, @ParentId, 'localhost', 1433, ...
);

-- 3. Insert nested properties (optional key-value table)
INSERT INTO cfg.{Implementation}Authentication (ConfigurationId, Name, Value)
VALUES
    (@ChildId, 'Username', 'app_user'),
    (@ChildId, 'SecretManagerName', 'EnvSecrets'),
    (@ChildId, 'SecretKeyName', 'DB_PASSWORD');
```

### Service Domains Requiring Database Tables

| ServiceCategory | Parent Table | Example Child Tables | Status |
|-----------------|--------------|---------------------|--------|
| Connection | conn.Connection | conn.MsSqlConnection, conn.HttpConnection | ✅ Complete |
| SecretManager | sec.SecretManager | sec.EnvironmentVariableSecretManager, sec.AzureKeyVaultSecretManager | ✅ Complete |
| Authentication | auth.Authentication | auth.JwtAuthentication, auth.BasicAuthentication, auth.OAuth2Authentication | ✅ Complete |
| Pipeline | pipe.Pipeline | pipe.BatchCopyPipeline, pipe.StreamingPipeline | ✅ Complete |
| Schedule | sched.Schedule | (no typed-body tables — CronExpression/IntervalSeconds on parent) | ✅ Complete |

### Troubleshooting

**"Configuration loaded: 0 entries"** - Usually means:
1. Parent table doesn't exist in database
2. `ParentTableName` is set but no parent ConfigurationType is registered
3. Child table has no FK column to parent table
4. Configuration seeding only inserted into child table, not parent

**"Secret manager not found"** - Usually means:
1. No `sec.SecretManager` parent table exists
2. `RegisterFactory` not registering the option's factory with the domain provider
3. SecretManager configurations not seeded into database

## Reference Implementations

### Connections (Primary Reference)
See the MsSql Connection implementation:
- [`Services.Connections.Abstractions`](../src/Fdw.Services.Connections.Abstractions/)
- [`Services.Connections`](../src/Fdw.Services.Connections/)
- [`Services.Connections.MsSql`](../src/Fdw.Services.Connections.MsSql/)

### DataStores (Three-Phase Pattern)
See the DataStore implementation for another complete example:
- [`Services.Data.Abstractions`](../src/Fdw.Services.Data.Abstractions/) - `IDataStoreServiceType`, `IDataStoreFactory`, `IDataStoreConfiguration`
- [`Services.Data`](../src/Fdw.Services.Data/) - `DataStoreServiceTypes`, `DefaultDataStoreProvider`
- [`Data.DataStores.SqlServer`](../src/Fdw.Data.DataStores.SqlServer/) - `MsSqlDataStoreServiceType`
- [`Data.DataStores.Rest`](../src/Fdw.Data.DataStores.Rest/) - `RestDataStoreServiceType`

**Key Pattern Differences:**
- DataStores have `TranslatorType` for command translation (MsSql → SQL, Rest → HTTP)
- DataStores include a `StoreTypeName` property for discriminating store types
- Registration includes `AddDefaultDataStoreProvider(configuration)` for DataSet integration
