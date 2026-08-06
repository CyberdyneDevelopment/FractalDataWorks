# FractalDataWorks .NET Templates

This directory contains dotnet templates for creating FractalDataWorks service domains and implementations using the factory instance pattern.

## Templates

### fdw-service-domain

Creates a new service domain with the DefaultServiceNameProvider implementation.

**Usage:**
```bash
dotnet new fdw-service-domain -ServiceName Connection
```

**Creates:**
- `DefaultServiceNameProvider.cs` - Provider with factory instance registration

### fdw-service-impl

Creates a new service implementation (ServiceType, Factory interface, and Factory).

**Usage:**
```bash
dotnet new fdw-service-impl -ImplName MsSql -ServiceName Connection
```

**Creates:**
- `ImplNameServiceNameType.cs` - ServiceType with two-phase registration
- `IImplNameServiceNameFactory.cs` - Factory interface
- `ImplNameServiceNameFactory.cs` - Factory implementation

## Installation

Install the templates locally:

```bash
cd public/templates/dotnet
dotnet new install .
```

List installed templates:

```bash
dotnet new list fdw
```

Uninstall templates:

```bash
dotnet new uninstall .
```

## Architecture Pattern

These templates implement the **factory instance pattern** with two-phase registration:

### Phase 1: RegisterRequiredServices
Each ServiceType registers:
1. The factory itself: `services.AddSingleton<IMyFactory, MyFactory>()`
2. All factory dependencies (translators, validators, IHttpClientFactory, etc.)

### Phase 2: RegisterFactory
Each ServiceType:
1. Resolves the factory from DI: `services.GetRequiredService<IMyFactory>()`
2. Registers the factory instance with the provider: `provider.RegisterFactory("MyType", factory)`

### Key Principles

1. **NO SERVICE LOCATOR** - Never inject `IServiceProvider` into runtime code
2. **FACTORY IS SINGLETON** - Factory registered in DI, dependencies injected via constructor
3. **CONFIGURATION IS RUNTIME** - Bound from value bag at runtime, passed to `Factory.Create()`
4. **EACH SERVICETYPE OWNS ITS REGISTRATIONS** - ServiceType registers its factory AND all dependencies

### Provider Pattern

The provider stores factory **instances** (not `Func<>`):

```csharp
// Factory instances - NOT Func<>
private readonly Dictionary<string, IServiceNameFactory> _factories;

// Register factory instance
public void RegisterFactory(string name, IServiceNameFactory factory)
{
    _factories[name] = factory;
}

// Use factory directly (no invocation needed)
var factory = _factories[typeName];
return await factory.CreateService(configuration);
```

## Example: Creating a New HTTP Connection Type

### 1. Create the Implementation Type

```bash
dotnet new fdw-service-impl -ImplName Http -ServiceName Connection
```

### 2. Update HttpConnectionType.cs

```csharp
public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
{
    RegisterConfiguration(services);

    // Register IHttpClientFactory dependency
    services.AddHttpClient();

    // Register the factory
    services.AddSingleton<IHttpConnectionFactory, HttpConnectionFactory>();

    return services;
}

public override void RegisterFactory(DefaultConnectionProvider provider, IServiceProvider services)
{
    // Resolve factory from DI
    var factory = services.GetRequiredService<IHttpConnectionFactory>();

    // Register instance with provider
    provider.RegisterFactory("Http", factory);
}
```

### 3. Update HttpConnectionFactory.cs

```csharp
public class HttpConnectionFactory : IHttpConnectionFactory
{
    private readonly ILogger<HttpConnectionFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Dependencies injected by DI
    public HttpConnectionFactory(
        ILogger<HttpConnectionFactory> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // Configuration passed at runtime
    public Task<IGenericResult<IGenericConnection>> CreateService(
        ConnectionConfiguration<HttpTypeConfiguration> configuration)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var connection = new HttpConnection(_logger, httpClient, configuration);
        return Task.FromResult(GenericResult<IGenericConnection>.Success(connection));
    }
}
```

## Configuration Pattern

### appsettings.json

```json
{
  "Connections": {
    "PaymentApi": {
      "ConnectionType": "Http",
      "Configuration": {
        "BaseUrl": "https://api.payments.com",
        "Timeout": 30
      }
    }
  }
}
```

### Runtime Binding

The provider binds the value bag to the concrete configuration type at runtime:

1. Get header from `IOptionsMonitor`
2. Get ServiceType for the configuration type
3. Bind value bag to concrete type using reflection/JSON
4. Get factory instance and call `CreateService(config)`

## Debugging Tips

1. **Factory not found in provider**: Check `[ServiceTypeOption]` name matches
2. **Factory constructor fails**: Check all dependencies registered in `RegisterRequiredServices`
3. **Configuration null/wrong type**: Check value bag has correct property names
4. **Hot-reload not working**: Verify provider subscribes to `IOptionsMonitor.OnChange`
5. **IHttpClientFactory null**: Verify `services.AddHttpClient()` in Phase 1

## Template Variables

### fdw-service-domain
- `ServiceName` - The service domain name (e.g., "Connection", "Notification")

### fdw-service-impl
- `ImplName` - The implementation name (e.g., "MsSql", "Http", "Email")
- `ServiceName` - The service domain name (e.g., "Connection", "Notification")

## File Naming Convention

Templates use the following naming patterns:

- Provider: `Default{ServiceName}Provider.cs`
- ServiceType: `{ImplName}{ServiceName}Type.cs`
- Factory Interface: `I{ImplName}{ServiceName}Factory.cs`
- Factory Implementation: `{ImplName}{ServiceName}Factory.cs`
- Service: `{ImplName}{ServiceName}.cs`
- Configuration: `{ImplName}TypeConfiguration.cs`
