# Quick Start

This guide walks through building and running the Reference Solution samples.

## Clone and Build

The Reference Solution contains concept samples and integration samples. Build individual concept samples:

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

Expected output (from [`Program.cs`](../../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Program.cs)):
```
=== FractalDataWorks TypeCollections Reference ===

1. TypeCollection (Basic, Immutable)
   - Compile-time discovered options
   - FrozenDictionary for O(1) lookups
   - Singleton instances

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
| `concepts/04-results` | Reference.Results | IGenericResult patterns |
| `concepts/05-configuration` | Reference.Configuration | ManagedConfiguration patterns |
| `concepts/06-data-layer` | Reference.DataLayer | DataGateway and commands |
| `concepts/07-translators` | Reference.Translators | Data translation patterns |
| `integration/01-pipeline` | Reference.Pipeline | Combined pipeline execution |
| `integration/02-custom-connection` | Reference.CustomConnection | Custom connection implementation |
| `integration/03-full-translator` | Reference.FullTranslator | Complete translator example |
| `integration/04-three-phase-di` | Reference.ThreePhaseDi | Three-phase DI registration |

## Building an API

FractalDataWorks uses **FastEndpoints** with **Scalar** for API development. Here's how to set up an API project:

### 1. Create the Project

```bash
cd public/samples/ReferenceSolution/api
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
dotnet add package FractalDataWorks.Services.Connections
dotnet add package FractalDataWorks.Services.Connections.MsSql
dotnet add package FractalDataWorks.Configuration.MsSql
```

### 3. Bootstrap Pattern

The configuration database connection is created **directly** using the factory (not via ConnectionProvider) since it's needed to load all other configurations:

```csharp
// Create bootstrap logger factory for MessageLogging during startup
using var bootstrapLoggerFactory = new SerilogLoggerFactory(Log.Logger);
var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("Reference.Api.Bootstrap");

// Load ConfigurationDb config from appsettings.json
var configDbConfig = builder.Configuration
    .GetSection("ConfigurationDb")
    .Get<MsSqlConnectionConfiguration>()!;

// Create secret manager factory for password resolution
var secretManagerFactory = new EnvironmentVariableSecretManagerFactory(
    bootstrapLoggerFactory.CreateLogger<EnvironmentVariableSecretManagerFactory>());
var secretManagerResult = await secretManagerFactory.Create(new EnvironmentVariableConfiguration());
var secretManager = secretManagerResult.Value;

// Create connection factory
var connectionFactory = new MsSqlConnectionFactory(
    bootstrapLoggerFactory.CreateLogger<MsSqlConnectionFactory>(),
    bootstrapLoggerFactory.CreateLogger<MsSqlConnection>());

// Create bootstrap connection - factory accepts ISecretManager directly
var connectionResult = await connectionFactory.Create(
    configDbConfig, secretManager, bootstrapLoggerFactory);

if (!connectionResult.IsSuccess)
{
    ConfigurationMsSqlLogger.ConfigurationDbConnectionFailed(
        bootstrapLogger,
        connectionResult.CurrentMessage ?? "Unknown error");
}
else
{
    using var bootstrapConnection = (MsSqlConnection)connectionResult.Value!;
    var connectResult = await bootstrapConnection.Connect();

    if (connectResult.IsSuccess)
    {
        builder.Configuration.AddMsSqlConfiguration(bootstrapConnection, options =>
        {
            options.SectionName = "Connections";
            options.ServiceCategory = "Connection";
        });
    }
}

// Three-phase registration
ConnectionTypes.Configure(builder.Services, builder.Configuration, bootstrapLoggerFactory);
ConnectionTypes.Register(builder.Services, bootstrapLoggerFactory);
```

### 4. MessageLogging (Never Use Log.Warning)

Always use MessageLogging instead of direct Serilog calls:

```csharp
// WRONG - direct Serilog
Log.Warning("Could not connect: {Error}", error);

// CORRECT - MessageLogging
ConfigurationMsSqlLogger.ConfigurationDbConnectionFailed(logger, error);
```

### 5. FastEndpoints Response Methods (v7.x)

FastEndpoints 7.x uses the `Send` property for responses:

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
cd public/samples/ReferenceSolution/api/Reference.Api
dotnet run
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
