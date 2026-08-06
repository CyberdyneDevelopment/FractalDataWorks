# FractalDataWorks Developer's Guide

A comprehensive guide for building enterprise applications with FractalDataWorks.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Universal Data Commands** | Same command interface for SQL, REST, files, and federated sources |
| **Fluent Builders** | Type-safe, IntelliSense-enabled query builders with chainable API |
| **Railway-Oriented Results** | `IGenericResult<T>` replaces exceptions for predictable error handling |
| **Source-Generated Types** | Zero-reflection TypeCollections discovered at compile-time |
| **Plugin Architecture** | Any data command runs through any connection type |
| **Structured Logging** | MessageLogging logs AND returns errors in one call |

---

## Table of Contents

- [Key Features](#key-features)
- [Quick Start](#quick-start)
- [Architecture Overview](#architecture-overview)
- [Core Patterns](#core-patterns)
  - [TypeCollections](#typecollections)
  - [ServiceTypeCollections](#servicetypecollections)
  - [Result Pattern](#result-pattern)
  - [MessageLogging](#messagelogging)
- [Data Access](#data-access)
  - [DataCommands](#datacommands)
  - [Filters and Queries](#filters-and-queries)
  - [DataGateway](#datagateway)
  - [Federated Datasets](#federated-datasets)
- [Service Domains](#service-domains)
  - [Three-Phase Registration](#three-phase-registration)
  - [Creating a Service Domain](#creating-a-service-domain)
- [Source Generators](#source-generators)
- [Configuration](#configuration)
- [Database Schema](#database-schema)
- [Reference](#reference)

---

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- PowerShell 7 (`pwsh`)
- Visual Studio 2022 / Rider / VS Code

### Build Commands

```bash
dotnet build -c Debug      # Fast iteration, no analyzers
dotnet build -c Release    # Production - must have 0 warnings
dotnet test                # Run all tests
```

### Key Packages

| Package | Purpose |
|---------|---------|
| `Fdw.Results` | Railway-oriented `IGenericResult<T>` |
| `Fdw.Collections` | TypeCollections with source generators |
| `Fdw.Services.Connections` | Database and API connections |
| `Fdw.Commands.Data` | Universal data commands |

---

## Architecture Overview

FractalDataWorks provides a layered architecture for enterprise applications:

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  (Your services, controllers, Blazor components)            │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer                            │
│  ConnectionProvider, AuthProvider, DataGateway, etc.        │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     Abstractions Layer                       │
│  TypeCollections, Interfaces, Results, Configuration        │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **No Reflection** - Use source-generated TypeCollections instead of runtime type discovery
2. **No Service Locator** - Inject dependencies via constructor, never store `IServiceProvider`
3. **No Exceptions for Flow Control** - Use `IGenericResult<T>` for expected failures
4. **Catch, Log, Return** - Never rethrow exceptions; use MessageLogging to log AND return

---

## Core Patterns

### TypeCollections

TypeCollections replace enums with extensible, type-safe alternatives that support cross-assembly discovery.

**Four Required Components:**

```csharp
// 1. Interface (in Abstractions project) — the marker is keyed off the interface itself
public interface IFilterOperator : ITypeOption<int, IFilterOperator> { }

// 2. Base Class (CRTP pattern - passes itself as TSelf)
public abstract class FilterOperatorBase : TypeOptionBase<int, FilterOperatorBase>, IFilterOperator
{
    protected FilterOperatorBase(int id, string name) : base(id, name) { }
}

// 3. Collection Class (partial - generator populates)
[TypeCollection(typeof(FilterOperatorBase), typeof(IFilterOperator), typeof(FilterOperators))]
public abstract partial class FilterOperators : TypeCollectionBase<FilterOperatorBase, IFilterOperator> { }

// 4. TypeOptions (can be in any assembly)
[TypeOption(typeof(FilterOperators), "Equal")]
public sealed class EqualOperator : FilterOperatorBase
{
    public EqualOperator() : base(1, "Equal") { }
}
```

**Usage:**

```csharp
// Lookup by name
var op = FilterOperators.ByName("Equal");

// Lookup by ID
var op = FilterOperators.ById(1);

// Get all
foreach (var op in FilterOperators.All()) { ... }
```

**Six TypeCollection Variants:**

| Variant | Use Case |
|---------|----------|
| `TypeCollection` | Immutable options (operators, states) |
| `MutableTypeCollection` | Runtime-extensible options |
| `TypeInstanceCollection` | Singleton instances per option |
| `ServiceTypeCollection` | Service plugins with DI registration |
| `MutableServiceTypeCollection` | Runtime-extensible service plugins |
| `ServiceTypeInstanceCollection` | Scoped service instances |

**See:** [TypeCollections Guide](../wiki/04-01-Overview.md) | [Patterns Reference](../wiki/10-TypeCollection-Patterns.md)

---

### ServiceTypeCollections

ServiceTypeCollections extend TypeCollections for service registration with dependency injection.

```csharp
[ServiceTypeCollection(
    typeof(ConnectionTypeBase<IGenericConnection, IConnectionFactory<IGenericConnection, ConnectionConfiguration>, ConnectionConfiguration>),
    typeof(IConnectionType),
    typeof(ConnectionTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IGenericConnection),
    ConfigurationType = typeof(ConnectionConfiguration),
    ProviderType = typeof(DefaultConnectionProvider),
    ProviderInterface = typeof(IConnectionProvider),
    ServiceCategory = "Connection")]
public partial class ConnectionTypes : ServiceTypeCollectionBase<
    ConnectionTypeBase<IGenericConnection, IConnectionFactory<IGenericConnection, ConnectionConfiguration>, ConnectionConfiguration>,
    IConnectionType<IGenericConnection, ConnectionConfiguration, IConnectionFactory<IGenericConnection, ConnectionConfiguration>>>
{ }
```

**ServiceTypeOption:** concrete options live in the implementation assembly
(e.g. `MsSqlConnectionType` in `Fdw.Services.Connections.MsSql`)
and are registered via the `[ModuleInitializer]` emitted by
`Fdw.Registration.SourceGenerators` in each entry-point project.

**See:** [ServiceTypes Guide](../wiki/06-02-Creating-Service-Domain.md)

---

### Result Pattern

FractalDataWorks uses railway-oriented programming with `IGenericResult<T>` instead of exceptions.

```csharp
public IGenericResult<User> GetUser(int id)
{
    if (id <= 0)
        return GenericResult<User>.Failure(UserCodes.ByName("InvalidId"));

    var user = _repository.Get(id);
    if (user == null)
        return GenericResult<User>.Failure(UserLog.UserNotFound(_logger, id));

    return GenericResult<User>.Success(user);
}
```

**Two Failure Approaches:**

| Approach | When to Use |
|----------|-------------|
| **MessageLogging** | Operational failures that need logging |
| **ResultCodes** | Validation failures, user feedback (no logging) |

```csharp
// MessageLogging - logs AND returns
return GenericResult.Failure(DomainLog.OperationFailed(_logger, ex, context));

// ResultCodes - returns only, no logging
return GenericResult.Failure(ValidationCodes.ByName("InvalidInput"));
```

---

### MessageLogging

MessageLogging generates structured logging methods that log AND return messages in one call. The source generator produces a `Code` value in the format `"{TypeCode}-{EventId}"` (e.g., `"FDW-5001"`). TypeCode defaults to `FDW` and can be customized per-method.

```csharp
/// <summary>
/// MessageLogging for Connection operations.
/// EventId range: 5000-5999
/// </summary>
public static partial class ConnectionLog
{
    [MessageLogging(EventId = 5001, Level = LogLevel.Error,
        Message = "Connection '{name}' failed: {error}")]
    public static partial IGenericMessage ConnectionFailed(
        ILogger logger, string name, string error);
    // Generated Code: "FDW-5001"

    // Exception parameter comes AFTER ILogger
    [MessageLogging(EventId = 5002, Level = LogLevel.Error,
        Message = "Connection failed")]
    public static partial IGenericMessage ConnectionException(
        ILogger logger, Exception ex, string context);

    // Custom TypeCode (char[] property, 2-6 uppercase alphanumeric)
    [MessageLogging(EventId = 5003, Level = LogLevel.Error,
        Message = "Custom domain error",
        TypeCode = new[] { 'C', 'N', 'X' })]
    public static partial IGenericMessage CustomDomainError(ILogger logger);
    // Generated Code: "CNX-5003"
}
```

**EventId Ranges:** the authoritative allocation table lives at the repo root in
[`RESULTCODE-CATALOG.md`](../../RESULTCODE-CATALOG.md). Always consult it before
picking a new range — every domain log class owns a documented slot, and unused
ranges are listed at the bottom of that file.

**See:** [MessageLogging Guide](../wiki/07-02-MessageLogging-Attribute.md)

---

## Data Access

### DataCommands

DataCommands provide a universal interface for data operations across SQL, REST, files, and more. Two APIs are available: object initialization for full control, or fluent builders for readable code.

**Fluent Builder (Preferred):**

```csharp
// Type-safe with IntelliSense. Query.From requires (dataStoreName, pathName, containerName).
var command = Query.From<Customer>("AppDb", "dbo", "Customers")
    .Where(c => c.IsActive).Equal(true)
    .Where(c => c.Name).StartsWith("A")
    .OrderBy(c => c.Name)
    .Skip(0).Take(50)
    .Build();

var result = await dataGateway.Execute(command);
```

**Object Initialization (Full Control):**

```csharp
var command = new QueryCommand<Customer>("Customers")
{
    Filter = new FilterExpression
    {
        Root = new FilterCondition
        {
            PropertyName = "IsActive",
            Operator = FilterOperators.ByName("Equal"),
            Value = true
        }
    },
    Ordering = new OrderingExpression
    {
        OrderedFields = [new OrderedField { PropertyName = "Name", Direction = SortDirection.Ascending }]
    },
    Paging = new PagingExpression { Skip = 0, Take = 50 }
};

var result = await dataGateway.Execute(command);
```

> Routing to a connection is resolved through the container/datastore registration
> (see DataStores/DataPaths). Commands do not carry a `ConnectionName` property.

**All Command Types Support Both APIs:**

| Command | Fluent Entry Point | Object Type |
|---------|-------------------|-------------|
| Query | `Query.From<T>(...)` | `QueryCommand<T>` |
| Insert | `Insert.Into<T>(...)` | `InsertCommand<T>` |
| Update | `Update.In<T>(...)` | `UpdateCommand<T>` |
| Delete | `Delete.From(...)` | `DeleteCommand` |

---

### Filters and Queries

Filters use a hierarchical tree structure supporting unlimited nesting. Fluent builders make complex filters readable.

**Simple Filter - Fluent:**

```csharp
var command = Query.From<Customer>("AppDb", "dbo", "Customers")
    .Where(c => c.IsActive).Equal(true)
    .Build();
// SQL: WHERE [IsActive] = @p0
// OData: $filter=IsActive eq true
```

**Simple Filter - Object:**

```csharp
var filter = new FilterExpression
{
    Root = new FilterCondition
    {
        PropertyName = "IsActive",
        Operator = FilterOperators.ByName("Equal"),
        Value = true
    }
};
```

**Complex Filter - Fluent (nested groups):**

```csharp
// (Name = 'Acme' OR Name = 'Corp') AND IsActive = true
var command = Query.From<Customer>("AppDb", "dbo", "Customers")
    .BeginOrGroup()
        .Where(c => c.Name).Equal("Acme")
        .Where(c => c.Name).Equal("Corp")
    .EndGroup()
    .Where(c => c.IsActive).Equal(true)
    .Build();
```

**Complex Filter - Object (nested groups):**

```csharp
// (Name = 'Acme' OR Name = 'Corp') AND IsActive = true
var filter = new FilterExpression
{
    Root = new FilterGroup
    {
        Operator = LogicalOperator.And,
        Nodes = new IFilterNode[]
        {
            new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes = new IFilterNode[]
                {
                    new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Equal"), Value = "Acme" },
                    new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Equal"), Value = "Corp" }
                }
            },
            new FilterCondition { PropertyName = "IsActive", Operator = FilterOperators.ByName("Equal"), Value = true }
        }
    }
};
```

**Available Operators:**

| Operator | SQL | OData |
|----------|-----|-------|
| `Equal` | `=` | `eq` |
| `NotEqual` | `<>` | `ne` |
| `GreaterThan` | `>` | `gt` |
| `LessThan` | `<` | `lt` |
| `Contains` | `LIKE '%x%'` | `contains()` |
| `StartsWith` | `LIKE 'x%'` | `startswith()` |
| `IsNull` | `IS NULL` | `eq null` |

---

### DataGateway

DataGateway routes commands to the appropriate connection based on configuration.

```
User Code
  ↓
IDataCommand (universal)
  ↓
DataGateway (routes by ConnectionName)
  ↓
IDataConnection (MsSql, Http, File)
  ↓
Translator (IDataCommand → SqlCommand/HttpRequest)
  ↓
Native Execution
```

**Usage:**

```csharp
public class CustomerService
{
    private readonly IDataGateway _gateway;

    public async Task<IGenericResult<IEnumerable<Customer>>> GetActive()
    {
        var command = new QueryCommand<Customer>("Customers")
        {
            Filter = new FilterExpression { /* ... */ }
        };
        return await _gateway.Execute(command);
    }
}
```

---

### DataSets

A DataSet is a logical schema (`DataSetConfiguration`) with one or more
**source bindings** (`DataSetSourceConfiguration`) attached to it. A source
binding points at a DataStore/DataPath/DataContainer plus a list of field
mappings. The same DataSet can be backed by SQL, an HTTP endpoint, a file,
or a mix of sources (federation).

```
DataSetConfiguration (logical schema: Fields, Name, RecordTypeName, …)
    └── Sources : IList<DataSetSourceConfiguration>
            ├── DataStoreName / DataPathName / DataContainerName  (where to read)
            └── DataSetType                                       (Standard | Compound | Federated)
```

A DataSet is queried like any other container:

```csharp
var command = new QueryCommand<CustomerOrderDto>("CustomerOrders")
{
    Filter = new FilterExpression { /* ... */ }
};
var result = await dataGateway.Execute(command);
```

**See:** [DataSets Guide](../wiki/05-02-DataSets.md) | [DataGateway Pattern](../wiki/05-01-DataGateway-Pattern.md)

---

## Service Domains

### Three-Phase Registration

All ServiceTypeCollections use three-phase DI registration:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Phase 1a: Configure (bind IOptions from config)
ConnectionTypes.Configure(builder.Services, builder.Configuration, loggerFactory);

// Phase 1b: Register (register factories with DI)
ConnectionTypes.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 2: Initialize (eager resolve for fail-fast)
ConnectionTypes.Initialize(app.Services, loggerFactory);

app.Run();
```

**Bootstrap order:** `SecretManagers` → `Connections` → `Authentication` → `Pipelines`.
Other domains (DataStores, Schedules, Notifications, etc.) follow the same
three-phase contract and are registered after the bootstrap chain.

---

### Creating a Service Domain

Each service domain requires three packages:

```
Services.{Domain}.Abstractions/     # Interfaces, base classes
├── I{Domain}.cs                    # Service interface
├── I{Domain}Configuration.cs       # Config interface
├── I{Domain}Factory.cs             # Factory interface
├── I{Domain}Provider.cs            # Provider interface
└── {Domain}TypeBase.cs             # ServiceType base

Services.{Domain}/                  # Collection, provider
├── {Domain}Types.cs                # ServiceTypeCollection
├── Default{Domain}Provider.cs      # Generated provider
└── Logging/{Domain}Log.cs          # MessageLogging

Services.{Domain}.{Impl}/           # Concrete implementations
├── {Impl}Type.cs                   # ServiceTypeOption
├── {Impl}Factory.cs                # Factory implementation
├── {Impl}Configuration.cs          # ManagedConfiguration
└── Logging/{Impl}Log.cs            # Impl-specific logging
```

**See:** [Creating Service Domains](../wiki/06-02-Creating-Service-Domain.md)

---

## Source Generators

### Generator Reference Format

Source generators MUST be referenced as analyzers:

```xml
<!-- For internal projects (within solution) -->
<ProjectReference Include="..\..\Fdw.Collections.SourceGenerators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

**Critical:** Without `OutputItemType="Analyzer"`, generators won't run and builds fail silently.

### Cross-Project Discovery

The generator runs multiple times across project boundaries:

1. **Abstractions compiles** → Generator discovers TypeCollection definition
2. **Implementation compiles** → Generator discovers TypeOptions, augments collection
3. **Consumer compiles** → Generator includes all discovered TypeOptions

This enables plugin architectures where downstream packages add TypeOptions to upstream collections.

### Common Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| TypeOptions not discovered | Missing analyzer reference | Add `OutputItemType="Analyzer"` |
| Build succeeds, nothing generated | Wrong reference format | Add `ReferenceOutputAssembly="false"` |
| Cross-project broken | Missing generator reference | Ensure all projects reference generator |

---

## Configuration

### ManagedConfiguration

Database-backed configuration classes use the `[ManagedConfiguration]` attribute:

```csharp
// Polymorphic-config pattern: identity-only parent (conn.Connection) + a typed-body
// child (conn.MsSqlConnection) joined via ConnectionId. The attribute only carries
// ServiceCategory/ServiceType; schema/table come from the DataContainer registration.
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "MsSql")]
public partial class MsSqlConnectionConfiguration : IConnectionConfiguration
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }  // FK to conn.Connection.Id
    public string Server { get; set; }
    public string Database { get; set; }
    public bool Encrypt { get; set; }
}
```

**Critical Rules:**
- Use `{ get; set; }` - IOptions binding requires mutable properties
- Use `partial class` - source generator adds members; use `sealed partial` for leaf classes
- Don't use `record class` - records use init-only setters that break binding
- Use `IList<T>` for collections - `IReadOnlyList<T>` breaks binding

**See:** [ManagedConfiguration Guide](../wiki/03-01-ManagedConfiguration.md)

### Configuration Loading

Each entry-point app ships a `configurationSchema.json` that declares the
ConfigurationDb connection plus the container/field/key metadata needed to
reach it. Once that bootstrap connection is live, the rest of the config flows
out of the database automatically:

```
configurationSchema.json (ships with the app)
    ↓ loaded into IConfiguration at the top of Program.cs
ConfigurationDb connection + container metadata
    ↓ MsSqlConfigurationSource queries [conn|data|pipe|sched|…].* tables
IConfiguration (key-value pairs, WHERE IsCurrent=1 AND IsDeleted=0)
    ↓ IOptions binding
IOptionsMonitor<List<TConfiguration>>
    ↓ ServiceConfigurationMonitor
DefaultConnectionProvider / DefaultPipelineProvider / …
```

**Never create custom configuration loaders** — use the three-phase
`Configure` → `Register` → `Initialize` pattern.

---

## Database Schema

Runtime configuration lives in **ConfigurationDb** (one database, many schemas).
Operations data lives in **OpsDb**; tenant/user identity lives in **AuthDb**.
The legacy `ctrl`/ControlDb dual-source schema has been removed — bootstrap
metadata (the ConfigurationDb connection itself, its container/field/key
catalogue, and secret-manager declarations) now ships as `configurationSchema.json`
with each entry-point app.

Top-level ConfigurationDb schemas:

| Schema | Purpose |
|--------|---------|
| **auth** | Local authentication metadata |
| **tenant** | Tenants, tenant features, settings |
| **conn** | Connections (identity-only parent + typed-body children per provider) |
| **data** | DataStores, DataPaths, DataContainers, Fields, Keys, DataSets, mappings |
| **pipe** | Pipelines, pipeline configurations |
| **sched** | Schedules, triggers |
| **quality** | Data-quality rules and results |
| **catalog** | Catalog metadata |
| **calc** | Calculations |
| **notify** | Notifications |
| **settings** | Generic key/value settings |
| **sec** | Secret-manager declarations |
| **authz** | Role-based access control |
| **transform** | Field mapping transforms |
| **workflow** | Workflow definitions |
| **agent** | Agent metadata |
| **audit** | Audit trail |
| **usr** | User-owned configuration scopes |

Operations data:

| Database / Schema | Purpose |
|---|---|
| **OpsDb (ops, etl, sched, …)** | Execution items, workflow events, pipeline run history |
| **AuthDb (auth)** | Users, refresh tokens, personal access tokens |

> See `databases/DATABASE-MAP.md` (in the `databases` repo) for the authoritative,
> current list of databases, schemas, and tables.

### Table Conventions

```sql
CREATE TABLE [conn].[Connection]
(
    [RowId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),  -- Version PK
    [Id] UNIQUEIDENTIFIER NOT NULL,                               -- Logical identity (durable)
    [Name] VARCHAR(200) NOT NULL,
    [ServiceOptionType] VARCHAR(100) NOT NULL,
    [IsCurrent] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Connection] PRIMARY KEY ([RowId])
);
GO

-- Filtered unique index enforces one current per entity
CREATE UNIQUE INDEX [UX_Connection_Id_Current] ON [conn].[Connection]([Id]) WHERE [IsCurrent] = 1;
GO
```

**See:** [Database Schema Reference](../wiki/08-02-Database-Schema.md)

---

## Reference

### Project Naming

| Suffix | Purpose | Target |
|--------|---------|--------|
| `.Abstractions` | Interfaces, base classes | netstandard2.0 |
| `.SourceGenerators` | Roslyn generators | netstandard2.0 |
| `.Analyzers` | Code analysis | netstandard2.0 |
| (none) | Implementations | net10.0 |

### Anti-Patterns

| Don't | Do Instead |
|-------|------------|
| `GenericResult.Failure("string")` | Use MessageLogging or ResultCode |
| `_logger.LogError(...)` | Use MessageLogging methods |
| `private readonly IServiceProvider` | Inject specific dependencies |
| `Type.GetType(typeName)` | Use TypeCollection lookup |
| `catch (Exception) { throw; }` | Catch, log, return Result |
| `{ get; init; }` on config | Use `{ get; set; }` |

### Conventions

- **Async methods:** No `Async` suffix (e.g., `Execute()` not `ExecuteAsync()`)
- **Test naming:** PascalCase without underscores
- **Private fields:** `_camelCase`
- **File organization:** One primary type per file

### Further Reading

| Topic | Link |
|-------|------|
| TypeCollections | [wiki/04-01-Overview.md](../wiki/04-01-Overview.md) |
| Service Domains | [wiki/06-02-Creating-Service-Domain.md](../wiki/06-02-Creating-Service-Domain.md) |
| MessageLogging | [wiki/07-02-MessageLogging-Attribute.md](../wiki/07-02-MessageLogging-Attribute.md) |
| Data Access | [wiki/05-01-DataGateway-Pattern.md](../wiki/05-01-DataGateway-Pattern.md) |
| Database Schema | [wiki/08-02-Database-Schema.md](../wiki/08-02-Database-Schema.md) |
| Configuration | [wiki/03-01-ManagedConfiguration.md](../wiki/03-01-ManagedConfiguration.md) |
