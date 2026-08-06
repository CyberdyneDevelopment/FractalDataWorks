# FractalDataWorks Developer Kit

A comprehensive .NET toolkit for building enterprise applications with Railway-Oriented Programming, type-safe plugin architectures, and zero-overhead source generation.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-preview-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

## Overview

The FractalDataWorks Developer Kit provides foundational infrastructure for enterprise .NET applications, emphasizing:

- **Railway-Oriented Programming** - Explicit error handling with `IGenericResult<T>` instead of exceptions
- **Type-Safe Plugin Architecture** - Extensible systems via TypeCollections and ServiceTypes
- **Source Generation** - Zero-runtime-cost abstractions with Roslyn source generators
- **High Performance** - O(1) lookups with FrozenDictionary, minimal allocations
- **Cross-Assembly Discovery** - Plugin types discovered at compile-time across assemblies

## Key Features

### 🚂 Railway-Oriented Programming
Explicit error handling without exceptions:

```csharp
public IGenericResult<User> GetUser(int id)
{
    if (id <= 0)
        return GenericResult<User>.Failure("Invalid user ID");

    var user = await _repository.GetById(id);
    return user != null
        ? GenericResult<User>.Success(user)
        : GenericResult<User>.Failure($"User {id} not found");
}

// Compositional error handling
var email = await GetUser(123)
    .Map(user => user.Email)
    .Match(
        success: email => $"Email: {email}",
        failure: error => $"Error: {error}"
    );
```

### 🔌 TypeCollections (Plugin Architecture)
Cross-project type discovery enabling downstream extensibility:

```csharp
// Abstractions project - collection
[TypeCollection(typeof(SecurityMethodBase), typeof(ISecurityMethod), typeof(SecurityMethods))]
public partial class SecurityMethods : TypeCollectionBase<SecurityMethodBase, ISecurityMethod> { }

// Concrete project - base type
public abstract class SecurityMethodBase : ISecurityMethod { }

// Any project (even downstream!) - type option
[TypeOption(typeof(SecurityMethods), "CustomAuth")]
public sealed class CustomAuthMethod : SecurityMethodBase { }

// Usage with O(1) lookups
var all = SecurityMethods.All();           // FrozenSet<ISecurityMethod>
var jwt = SecurityMethods.Jwt;             // Static property (generated)
var custom = SecurityMethods.GetByName("CustomAuth");  // O(1) lookup
```

### 🎯 ServiceTypes (Service Plugin Framework)
High-performance service registration with progressive generic constraints:

```csharp
// Service type with dual inheritance pattern
[ServiceTypeOption(typeof(ConnectionTypes), "PostgreSql")]
public sealed class PostgreSqlConnectionType :
    ConnectionTypeBase<IGenericConnection, PostgreSqlConfiguration, IPostgreSqlConnectionFactory>,
    IConnectionType<IGenericConnection, PostgreSqlConfiguration, IPostgreSqlConnectionFactory>
{
    public PostgreSqlConnectionType() : base(
        name: "PostgreSql",
        sectionName: "PostgreSql",
        displayName: "PostgreSQL Database",
        description: "PostgreSQL connection",
        optionsLoader: new ScopedOptionsLoader(),
        category: "Database")
    { }

    public override void Register(IServiceCollection services)
    {
        services.AddScoped<IPostgreSqlConnectionFactory, PostgreSqlConnectionFactory>();
        services.AddScoped<PostgreSqlService>();
    }
}

// Automatic registration
ConnectionTypes.Register(services);
```

### ✨ TypeCollections (Type-Safe Plugins)
Type-safe, extensible alternative to enums with rich behavior and cross-assembly discovery:

```csharp
// Define collection
[TypeCollection(typeof(PriorityBase), typeof(IPriority), typeof(Priorities))]
public static partial class Priorities : TypeCollectionBase<PriorityBase, IPriority> { }

// Define base type
public abstract class PriorityBase : TypeOptionBase<Guid, PriorityBase>, IPriority
{
    public int UrgencyLevel { get; }

    protected PriorityBase(string name, int urgencyLevel) : base(name)
    {
        UrgencyLevel = urgencyLevel;
    }
}

// Concrete implementations
[TypeOption(typeof(Priorities), "Low")]
public sealed class LowPriority : PriorityBase
{
    public LowPriority() : base("Low", urgencyLevel: 1) { }
}

[TypeOption(typeof(Priorities), "High")]
public sealed class HighPriority : PriorityBase
{
    public HighPriority() : base("High", urgencyLevel: 10) { }
}

// Usage with O(1) lookups
var all = Priorities.All();                    // FrozenSet<IPriority>
var low = Priorities.ByName("Low");            // O(1) lookup
var high = Priorities.High;                    // Generated static property
```

### 📦 Universal Data Commands
Single API for data access across any backend (SQL, REST, Files, GraphQL):

```csharp
// Write once - using correct API patterns
var command = new QueryCommand<Customer>("Customers")
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = nameof(Customer.IsActive),
            Operator = FilterOperators.ByName("Equal"),  // Or FilterOperators.Equal
            Value = true
        }
    },
    Paging = new PagingExpression { Skip = 0, Take = 50 }
};

// Execute against any backend via translators
var sqlResult = await sqlConnection.ExecuteAsync(command);    // → T-SQL
var restResult = await restConnection.ExecuteAsync(command);  // → OData
var fileResult = await fileConnection.ExecuteAsync(command);  // → JSON filtering
```

### 🗺️ Logical-Physical Data Mapping
Separation of conceptual data definitions from physical storage:

```csharp
// Define logical dataset (what data conceptually is)
var salesDataSet = new DataSetConfiguration
{
    DataSetName = "Sales",
    Fields = new[]
    {
        new DataFieldConfiguration { Name = "CustomerID", Type = "int" },
        new DataFieldConfiguration { Name = "Amount", Type = "decimal" },
        new DataFieldConfiguration { Name = "OrderDate", Type = "datetime" }
    },

    // Map to multiple physical sources
    Sources = new Dictionary<string, SourceMappingConfiguration>
    {
        ["SQL_Primary"] = new()
        {
            Priority = 1,           // Primary source
            EstimatedCost = 10,     // Fast, cheap
            Sql = new SqlMappingConfiguration
            {
                SchemaName = "sales",
                TableName = "Orders",
                FieldMappings = new Dictionary<string, string>
                {
                    ["CustomerID"] = "cust_id",
                    ["Amount"] = "total_amount",
                    ["OrderDate"] = "order_dt"
                }
            }
        },
        ["REST_Backup"] = new()
        {
            Priority = 2,           // Fallback source
            EstimatedCost = 50,     // Slower, more expensive
            Http = new HttpMappingConfiguration
            {
                Endpoint = "/api/v2/orders",
                FieldMappings = new Dictionary<string, string>
                {
                    ["CustomerID"] = "customerId",
                    ["Amount"] = "orderAmount",
                    ["OrderDate"] = "orderDate"
                }
            }
        }
    }
};

// Query using logical names - DataGateway handles the rest
var command = new QueryCommand<Order>("Sales")
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "Amount",  // Logical field
            Operator = FilterOperators.ByName("GreaterThan"),
            Value = 1000
        }
    }
};

// DataGateway flow:
// 1. Resolve "Sales" DataSet
// 2. Select best source (SQL_Primary: priority=1, available)
// 3. Resolve DataStore "SwRegionDb"
// 4. Build DataPath "SwRegionDb.sales.Orders"
// 5. Get DataContainer (physical schema)
// 6. Map "Amount" → "total_amount"
// 7. Translate to SQL: WHERE [total_amount] > @p0
// 8. Execute and return results
```

**Key Components:**
- **DataSet** - Logical/conceptual definition (WHAT data is)
- **DataStore** - Physical location (WHERE data lives)
- **DataPath** - Navigation within store (HOW to find data)
- **DataContainer** - Physical schema (WHAT format data is in)
- **SourceMappingConfiguration** - Maps logical fields to physical fields with priority/cost

## Architecture

### Core Framework Components

```
Foundation Layer (netstandard2.0)
├── Abstractions              # Core interfaces and base types
├── Results                   # Railway-oriented programming
├── Messages                  # Structured messaging
└── Configuration.Abstractions # Configuration contracts

Type System Layer (netstandard2.0;net10.0)
├── Collections               # TypeCollection framework
└── ServiceTypes             # ServiceType plugin framework

Source Generation Layer (netstandard2.0)
├── Collections.SourceGenerators    # TypeCollection AND ServiceType code generation
├── MessageLogging.SourceGenerators # Structured logging generation
└── Messages.SourceGenerators       # Message type generation

Code Quality Layer (netstandard2.0)
├── Collections.Analyzers     # Compile-time validation
├── Collections.CodeFixes     # Automatic fixes
├── ServiceTypes.Analyzers    # Pattern compliance
└── ServiceTypes.CodeFixes    # Automatic fixes

Service Infrastructure Layer (net10.0)
├── Services                  # Service base classes
├── Services.Abstractions     # Service contracts
├── Services.Execution        # Execution pipeline
├── Services.Connections      # Connection management
├── Services.Scheduling       # Job scheduling
├── Services.Authentication   # Authentication services
├── Services.SecretManagers   # Secret management
└── Services.Transformations  # Data transformations

Configuration Layer (netstandard2.0;net10.0)
├── Configuration.Abstractions         # Configuration contracts
├── Configuration                       # Configuration loading
├── Configuration.Writers.Abstractions # Configuration writer contracts
├── Configuration.Writers              # CQRS write-side for configuration
├── Configuration.Writers.MsSql        # SQL Server backend
└── Configuration.Writers.InMemory     # In-memory backend for testing

UI Layer (net10.0)
├── UI.Abstractions           # Page model interfaces (Dashboard, List, Detail, Tree, Wizard, LogViewer)
├── UI.Components             # Core component base classes (CRTP pattern)
├── UI.Components.Blazor      # Blazor components with Canvas widgets
├── UI.Rendering.Spectre      # Spectre.Console page renderers
└── TUI.Management            # Terminal UI management application

Data Layer (netstandard2.0;net10.0)
├── Commands.Data.Abstractions      # Universal command interfaces
├── Commands.Data                   # Command implementations
├── Data.DataSets.Abstractions      # Logical dataset definitions
├── Data.DataSets                   # DataSet implementations
├── Data.DataStores.Abstractions    # Physical storage abstractions
├── Data.DataStores                 # DataStore implementations
├── Data.DataContainers.Abstractions # Physical schema abstractions
├── Data.DataContainers             # DataContainer implementations
├── Data.Translators                # Backend-specific translators
└── Data.Sql                        # SQL utilities

Utilities Layer
├── CodeBuilder              # Code generation utilities
└── DependencyInjection      # DI extensions
```

### Project Organization

Projects follow consistent naming patterns:
- `*.Abstractions` - Interfaces and contracts only
- `*.SourceGenerators` - Roslyn source generators
- `*.Analyzers` - Compile-time code analysis
- `*.CodeFixes` - Automatic code fix providers

## Quick Start

### Installation

```bash
# Core packages
dotnet add package FractalDataWorks.Abstractions
dotnet add package FractalDataWorks.Results
dotnet add package FractalDataWorks.Collections

# Source generators (Collections.SourceGenerators handles BOTH TypeCollections AND ServiceTypes)
# The generator is embedded in the Collections package - just add that!
dotnet add package FractalDataWorks.Collections.SourceGenerators

# Service infrastructure
dotnet add package FractalDataWorks.Services
dotnet add package FractalDataWorks.Services.Abstractions
```

### Basic Example

```csharp
// Define configuration
public interface IEmailConfiguration : IGenericConfiguration
{
    string SmtpServer { get; }
    int Port { get; }
}

// Implement service with Railway-Oriented Programming
public class EmailService : ServiceBase<IEmailCommand, IEmailConfiguration, EmailService>
{
    public async Task<IGenericResult> SendEmail(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(to))
            return GenericResult.Failure("Recipient email is required");

        try
        {
            await _smtpClient.SendAsync(to, subject, body);
            return GenericResult.Success("Email sent successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send email");
            return GenericResult.Failure($"Failed to send email: {ex.Message}");
        }
    }
}
```

## Building

### Requirements

- .NET 10.0 SDK (or .NET 8.0+ for netstandard2.0 projects)
- C# preview language features enabled
- Visual Studio 2024, Rider 2024.3, or VS Code with C# extension

### Build Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Build with specific configuration
dotnet build -c Debug      # Fast iteration, minimal analyzers
dotnet build -c Alpha      # Basic checks
dotnet build -c Beta       # Full analyzers, warnings as errors
dotnet build -c Release    # Production build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Pack NuGet packages
dotnet pack -c Release -o ./artifacts
```

### Configuration Levels

The solution uses progressive build configurations (defined in `Directory.Build.props`):

- **Debug** - Fast builds, no analyzers (rapid development)
- **Experimental** - Minimal analyzers (prototyping)
- **Alpha** - Basic checks (early testing)
- **Beta** - Full analyzers, warnings as errors (quality gate)
- **Preview** - Strict checks (release candidate)
- **Release** - Production-ready with all validations

## Testing

Tests use xUnit v3 with Shouldly for assertions:

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/FractalDataWorks.Collections.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName~TypeCollection_GetByName_ReturnsCorrectType"

# Exclude integration tests
dotnet test --filter "Category!=Integration"
```

## Documentation

- **[Architecture Guide](discussions/ARCHITECTURE_SUMMARY.md)** - System architecture and design decisions
- **[Developer's Guide](docs/DevelopersGuide.md)** - Comprehensive development guide
- **[TypeCollections Guide](src/FractalDataWorks.Collections/README.md)** - Plugin architecture patterns
- **[ServiceTypes Guide](src/FractalDataWorks.Services.Abstractions/README.md)** - Service registration framework
- **[DataCommands Guide](src/FractalDataWorks.Commands.Data/README.md)** - Universal data access
- **[DataStore Architecture Guide](docs/DataStoreArchitecture.md)** - Logical-physical mapping pattern
- **[Results Guide](src/FractalDataWorks.Results/README.md)** - Railway-oriented programming
- **[Configuration Writers Guide](wiki/03-02-ConfigurationWriters.md)** - CQRS write-side for configuration persistence
- **[UI Components Guide](wiki/11-ui-components/README.md)** - UI framework documentation
  - [Page Types](wiki/11-ui-components/page-types.md) - Page model interfaces
  - [Page Renderers](wiki/11-ui-components/page-renderers.md) - Spectre and Blazor renderers
  - [Canvas Widgets](wiki/11-ui-components/canvas-widgets.md) - Blazor visualization components
  - [Creating Pages](wiki/11-ui-components/creating-pages.md) - Step-by-step guide

Each project contains a comprehensive README with implementation details and examples.

## Design Principles

### No Enums, No Switch Statements
Use TypeCollections instead:

```csharp
// ❌ Don't use enums
public enum FilterOperator { Equal, NotEqual, GreaterThan }

// ✅ Use TypeCollections
[TypeOption(typeof(FilterOperators), "Equal")]
public sealed class EqualOperator : FilterOperatorBase
{
    public EqualOperator() : base(name: "Equal", sqlOperator: "=") { }
}
```

### Railway-Oriented Over Exceptions
Return results instead of throwing for anticipated failures:

```csharp
// ❌ Don't throw for business logic
public User GetUser(int id)
{
    if (id <= 0) throw new ArgumentException("Invalid ID");
    return _repository.Get(id) ?? throw new NotFoundException();
}

// ✅ Return results
public IGenericResult<User> GetUser(int id)
{
    if (id <= 0)
        return GenericResult<User>.Failure("Invalid ID");

    var user = _repository.Get(id);
    return user != null
        ? GenericResult<User>.Success(user)
        : GenericResult<User>.Failure("User not found");
}
```

### Source Generation Over Reflection
Generate code at compile-time:

```csharp
// ❌ Don't use runtime reflection
var types = Assembly.GetTypes().Where(t => t.IsAssignableTo<IPlugin>());

// ✅ Use source generation
[TypeCollection(typeof(PluginBase), typeof(IPlugin), typeof(Plugins))]
public partial class Plugins : TypeCollectionBase<PluginBase, IPlugin> { }

var all = Plugins.All();  // FrozenSet populated at compile-time
```

## Performance

The framework is optimized for high performance:

- **O(1) Lookups** - FrozenDictionary/FrozenSet for collection operations
- **Zero Reflection** - Source generation eliminates runtime reflection
- **Minimal Allocations** - Value types and pooling where appropriate
- **Compile-Time Safety** - Catch errors at build time, not runtime
- **Fast Service Creation** - FastGenericNew for instantiation

Benchmark results show TypeCollections with FrozenDictionary outperform dictionary and array lookups by 2-3x.

## Contributing

We welcome contributions! Please:

1. Review the [Architecture Guide](discussions/ARCHITECTURE_SUMMARY.md)
2. Follow the patterns in [CLAUDE.md](.claude/CLAUDE.md)
3. Ensure all tests pass
4. Add tests for new functionality
5. Update relevant documentation

### Code Standards

- Target `netstandard2.0` for abstractions
- Multi-target `netstandard2.0;net10.0` for implementations where needed
- Use Railway-Oriented Programming (return `IGenericResult<T>`)
- No enums - use TypeCollections
- No switch statements - use properties and visitor pattern
- Source-generated logging with `[MessageLogging]` returning `IGenericMessage`
- Full XML documentation on public APIs

## License

Licensed under the Apache-2.0 License. See [LICENSE](LICENSE) for details.

## Acknowledgments

Built with modern .NET technologies:
- .NET 10.0 / .NET Standard 2.0
- C# preview language features
- Roslyn Source Generators
- xUnit v3
- Shouldly
- FluentValidation

---

**FractalDataWorks Developer Kit** - Building better software through type safety, explicit error handling, and zero-overhead abstractions.
