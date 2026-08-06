# Quick Start

This guide walks through building and running the Reference Solution samples.

## Clone and Build

The Reference Solution contains concept samples. Build an individual concept sample:

```bash
cd public/samples/ReferenceSolution/concepts/01-type-collections
dotnet build -c Release --warnaserror
```

## Run a Concept Sample

Each concept sample is a standalone console application. Run the TypeCollections sample:

```bash
cd public/samples/ReferenceSolution/concepts/01-type-collections
dotnet run --project src/Reference.TypeCollections
```

Expected output (from [`Program.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs)):
```
=== TypeCollections Demo ===

1. Basic TypeCollection (PaymentMethods)
   PaymentMethods.Cash: Cash, Fee: 0%
   PaymentMethods.ByName("CreditCard"): CreditCard
   All payment methods: Cash, CreditCard, BankTransfer
   $100 via CreditCard = $102.50
```

## Project Structure

The Reference Solution is organized into concept samples and integration samples:

| Folder | Sample | Purpose |
|--------|--------|---------|
| `concepts/01-type-collections` | Reference.TypeCollections | TypeCollection patterns |
| `concepts/02-service-types` | Reference.ServiceTypes | ServiceType with DI integration |
| `concepts/03-message-logging` | Reference.MessageLogging | MessageLogging attribute usage |
| `concepts/05-configuration` | Reference.Configuration | ManagedConfiguration patterns |
| `concepts/06-data-layer` | Reference.DataLayer | DataGateway and commands |

## Building an API

FractalDataWorks uses **FastEndpoints** with **Scalar** for API development. The full Reference.Api project has been moved to a separate repository (`reference-api`); the patterns below show how to set up an API project in your own solution.

### 1. Create the Project

```bash
dotnet new webapi -n Reference.Api
cd Reference.Api
```

### 2. Add Required Packages

Use `dotnet add package` to add packages (this ensures correct versions in central package management):

```bash
# FastEndpoints + Scalar
dotnet add package FastEndpoints
dotnet add package FastEndpoints.Swagger
dotnet add package Scalar.AspNetCore

# Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Extensions.Logging

# FractalDataWorks
dotnet add package Fdw.Services.Connections
dotnet add package Fdw.Services.Connections.MsSql
dotnet add package Fdw.Configuration.MsSql
```

### 3. Startup Pattern

The configuration database connection is created **directly** using the factory (not via ConnectionProvider) since it's needed to load all other configurations:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Logger factory (used during phase 1).
var loggerFactory = builder.AddFrameworkSerilog("Reference.Api");

// 2. JSON-driven configuration startup. Reads configurationSchema.json from the
//    app's content root and registers IConfigurationGateway.
builder.Services.AddConfigurationGateway<MsSqlConnectionFactory, EnvironmentVariableSecretManager>(
    "configurationSchema.json");

// 3. ONE PlatformServices sweep — every [ServiceTypeCollection] discovered by the
//    generated module initializer participates (ConnectionTypes among them). Each connection-kind
//    [ServiceTypeOption] registers ConnectionConfigurationProvider itself (idempotent) — no
//    hand-written RegisterDomainServices call here.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// 4. Initialize phase — runs after Build, before middleware. One sweep, dependency order.
PlatformServices.Initialize(app.Services, loggerFactory);
```

See [JSON-Driven Configuration Startup](03-05-JSON-Driven-Configuration.md) for the
shape of `configurationSchema.json` and [Configuration Provider Registration](03-05-Configuration-Provider-Registration-Pattern.md)
for the three-phase lifecycle.

### 4. MessageLogging (Never Use Log.Warning)

Always use MessageLogging instead of direct Serilog calls:

```csharp
// WRONG - direct Serilog
Log.Error(ex, "Could not load configuration for {Section}", sectionName);

// CORRECT - MessageLogging
ConfigurationMsSqlLogger.ConfigurationLoadFailed(logger, ex, sectionName);
```

### 5. FastEndpoints Response Methods (v8.x)

FastEndpoints 8.x uses the `Send` property for responses:

```csharp
public sealed class ListConnectionsEndpoint : EndpointWithoutRequest<List<ConnectionResponse>>
{
    public override void Configure()
    {
        Get("/connections");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var connections = new List<ConnectionResponse>();
        // ... populate connections ...

        await Send.OkAsync(connections, ct).ConfigureAwait(false);
    }
}
```

### 6. Run the API

```bash
dotnet run --project Reference.Api
```

Open `http://localhost:5149/scalar` for the Scalar API documentation UI.

### 7. API Endpoints

The Reference API includes these endpoints:

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| GET | `/api/connections` | List all configured connections |
| GET | `/api/connections/{name}` | Get connection details |
| POST | `/api/connections/{name}/test` | Test a connection |
| POST | `/api/connections/{name}/connect` | Connect and verify |
| GET | `/api/connection-types` | List available connection types |

### 8. Example Requests

**List connections:**
```bash
curl http://localhost:5149/api/connections
```

**Test a connection:**
```bash
curl -X POST http://localhost:5149/api/connections/ProductionDb/test
```

**Get available connection types:**
```bash
curl http://localhost:5149/api/connection-types
```

## Next Steps

- [What You Built](01-03-What-You-Built.md) - Understand the sample architecture
- [Project Layout](02-01-Project-Layout.md) - Framework project structure
