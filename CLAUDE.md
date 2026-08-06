# CLAUDE.md - FractalDataWorks Developer Kit

This file provides guidance to Claude Code when working with the FractalDataWorks repository.

## Quick Reference

| What | Where |
|------|-------|
| Build | `dotnet build -c Debug` (fast) / `dotnet build -c Release` (strict) |
| Test | `dotnet test` |
| Pack Local | `./scripts/pack-local.sh` |
| Wiki | `wiki/` - Structured guides |
| Reference Solution | `samples/ReferenceSolution/` |

## Available Specialists

Delegate complex tasks to these specialists via the Task tool:

| Specialist | Use For |
|------------|---------|
| `fdw-service-specialist` | Creating service domains (Connections pattern), three-phase DI, factories, providers |
| `fdw-typecollection-specialist` | Creating TypeCollections, base classes, CRTP pattern, TypeOptions |
| `fdw-configuration-specialist` | ManagedConfiguration classes, DDL generation, validation, IOptions patterns |
| `fdw-operations-specialist` | Execution tracking, workflow orchestration, state machines, escalation policies |
| `fdw-etl-specialist` | ETL pipelines, transforms, calculations, data transformations |
| `test-engineer` | xUnit v3, Shouldly tests following FDW patterns |
| `roslyn-generator-specialist` | Incremental source generators, analyzers, code fixes |

## Available Skills

| Skill | Description |
|-------|-------------|
| `/new-service-domain` | Scaffold complete service domain (3 packages) |
| `/new-typecollection` | Create TypeCollection with interface, base, options |
| `/register-provider` | Create MutableTypeCollection with Provider pattern |
| `/result-logging` | Create MessageLogging + ResultCodes with proper patterns |
| `/add-messagelog` | Add MessageLogging methods with proper event IDs |
| `/verify-patterns` | Audit code for FDW pattern compliance |
| `/add-service-impl` | Add implementation to existing service domain |
| `/execution-tracking` | Track workflow/job/task executions via IExecutionTracker |
| `/create-execution` | Create new execution items (Workflow, Job, Stage, Step, Task) |
| `/update-execution` | Update executions with state transitions, events, and completion |
| `/service-configuration` | Create ManagedConfiguration classes with DDL templates |
| `/data-commands` | Build Query/Insert/Update/Delete commands with fluent API |
| `/test-analysis` | Analyze code complexity and testability |
| `/test-patterns` | Write tests following FDW xUnit v3 + Shouldly patterns |

## Database Schema

Configuration is stored in **ConfigurationDb** (50 tables across 5 schemas).

### Schema Organization

| Schema | Tables | Purpose |
|--------|--------|---------|
| **cfg** | 34 | Configuration (connections, datasets, auth, tenants) |
| **etl** | 5 | ETL pipelines and execution |
| **sched** | 5 | Scheduling (cron, interval) |
| **ops** | 3 | Operations (escalation, workflows) |
| **types** | 3 | TypeCollection metadata |

### SQL Server Database Project

Schema is managed in `databases/ControlDb.sqlproj` (Microsoft.Build.Sql SDK 2.0.0).

```powershell
# Build dacpac
cd databases
dotnet build -c Release

# Deploy to Docker (Reference Solution)
cd samples/ReferenceSolutions/ApiSolution
./docker/mssql/build-dacpac.ps1
docker-compose -f docker-compose.dacpac.yml up -d
```

### Table Conventions

- **Parent-child FK pattern**: Child tables share PK with parent
- **Soft delete**: `IsCurrent`, `IsDeleted` columns
- **Extended properties**: `MS_Description` on all tables/columns
- **GO separators**: Required between statements in .sql files

See [08-02-Database-Schema](wiki/08-02-Database-Schema.md) for complete documentation.

## Critical Architecture Patterns

### 1. NO REFLECTION - Source-Generated Collections Only

```csharp
// WRONG
var type = Type.GetType(typeName);

// CORRECT
var factory = ConnectionTypes.GetFactory(typeName);
```

### 2. NO SERVICE LOCATOR - Never Store IServiceProvider

```csharp
// WRONG
private readonly IServiceProvider _services;

// CORRECT - Inject dependencies via constructor
public class MyFactory(ILoggerFactory loggerFactory, ISecretManagerProvider secretProvider)
```

### 3. CATCH, LOG, RETURN - Never Throw

```csharp
// CORRECT
try { ... }
catch (Exception ex)
{
    return GenericResult<T>.Failure(
        DomainLog.OperationFailed(_logger, ex, contextInfo));
}

// WRONG - never rethrow
catch (Exception ex) { throw; }
```

### 4. MessageLogging - Log AND Return

Every logged message is returned in the result. Every returned message is logged.

```csharp
// CORRECT - domain-specific message
return GenericResult.Failure(
    ConnectionLog.ConnectionNotFound(_logger, connectionName));

// WRONG - generic error
return GenericResult.Failure(MessageLog.Error("Connection not found"));
```

### 5. Three-Phase Registration

```csharp
// Phase 1 - ConfigureServices (before Build)
{Domain}Types.Configure(services, configuration, loggerFactory);

// Phase 2 - Register (before Build)
{Domain}Types.Register(services, loggerFactory);

// Phase 3 - Initialize (after Build, before Run)
{Domain}Types.Initialize(app.Services, loggerFactory);
```

## TypeCollection Pattern

**Four Required Components:**

```csharp
// 1. Interface (Abstractions)
public interface I{Name} : ITypeOption<int, {Name}Base> { }

// 2. Base Class (CRTP Pattern)
public abstract class {Name}Base : TypeOptionBase<int, {Name}Base>, I{Name}
{
    protected {Name}Base(int id, string name) : base(id, name) { }
}

// 3. Collection Class
[TypeCollection(typeof({Name}Base), typeof(I{Name}), typeof({Name}s))]
public abstract partial class {Name}s : TypeCollectionBase<{Name}Base, I{Name}> { }

// 4. Type Options
[TypeOption(typeof({Name}s), "OptionName")]
public sealed class OptionName{Name} : {Name}Base
{
    public OptionName{Name}() : base(1, "OptionName") { }
}
```

**Reference Implementations:**
- `src/FractalDataWorks.Services.Etl.Abstractions/OptionTypes/TransformTypes.cs`
- `src/FractalDataWorks.Data/FilterOperators.cs`

## ServiceTypeCollection Pattern

```csharp
[ServiceTypeCollection(
    typeof({Domain}TypeBase<...>),
    typeof(I{Domain}Type),
    typeof({Domain}Types),
    GenerateProvider = true,
    ServiceInterface = typeof(I{Domain}),
    ConfigurationInterface = typeof(I{Domain}Configuration),
    ProviderType = typeof(Default{Domain}Provider),
    ProviderInterface = typeof(I{Domain}Provider))]
public partial class {Domain}Types : ServiceTypeCollectionBase<...> { }
```

**Reference Implementations:**
- `src/FractalDataWorks.Services.Connections/ConnectionTypes.cs`
- `src/FractalDataWorks.Services.Data/DataStoreServiceTypes.cs`

## MessageLogging Pattern

```csharp
/// <summary>
/// MessageLogging for {Domain} operations.
/// EventId range: {NNNN}-{NNNN}
/// </summary>
public static partial class {Domain}Log
{
    [MessageLogging(
        EventId = {NNNN},
        Level = LogLevel.Information,
        Message = "Operation '{name}' started")]
    public static partial IGenericMessage OperationStarted(
        ILogger logger,
        string name);
}
```

**Event ID Ranges (Allocated):**
| Range | Domain |
|-------|--------|
| 5000-5999 | Connections |
| 6000-6099 | Secret Managers |
| 6100-6199 | Resiliency |
| 7000-7699 | Authentication |
| 7700-7799 | Operations (Execution Tracking, Escalation) |
| 8001-8099 | ServiceTypes |
| 8101-8199 | ETL |
| 9001-9099 | Roslyn Workspace |

**Reference Implementation:**
- `src/FractalDataWorks.Services.Etl/Logging/EtlLog.cs`

## Service Domain Structure

```
Services.{Domain}.Abstractions/    # Interfaces, base classes
├── I{Domain}.cs
├── I{Domain}Configuration.cs
├── {Domain}ConfigurationBase.cs
├── I{Domain}Factory.cs
├── I{Domain}Provider.cs
└── {Domain}TypeBase.cs

Services.{Domain}/                  # Provider, collection, registration
├── {Domain}Types.cs
├── Default{Domain}Provider.cs
└── Logging/{Domain}Log.cs

Services.{Domain}.{Implementation}/ # Concrete implementations
├── {Implementation}Type.cs
├── {Implementation}Factory.cs
├── {Implementation}Configuration.cs
└── Logging/{Implementation}Log.cs
```

**Reference Implementations:**
- Connections: `src/FractalDataWorks.Services.Connections.Abstractions/`, `Services.Connections/`, `Services.Connections.MsSql/`
- DataStores: `src/FractalDataWorks.Services.Data.Abstractions/`, `Services.Data/`, `Data.DataStores.SqlServer/`

## Build Commands

```bash
dotnet build -c Debug          # Fast iteration
dotnet build -c Release        # Production with analyzers
dotnet test                    # Run all tests
./scripts/pack-local.sh        # Pack for local development
```

## Project Naming Conventions

| Suffix | Purpose | Target |
|--------|---------|--------|
| `.Abstractions` | Interfaces only | netstandard2.0 |
| `.SourceGenerators` | Roslyn generators | netstandard2.0 |
| `.Analyzers` | Code analysis | netstandard2.0 |
| `.CodeFixes` | Automatic fixes | netstandard2.0 |
| (none) | Implementations | net10.0 |

## Source Generator References

**Internal projects must use:**
```xml
<ProjectReference Include="..\Collections.SourceGenerators\..."
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

**Critical:** Without `OutputItemType="Analyzer"`, generator won't run!

## Test Conventions

- **Framework:** xUnit v3
- **Assertions:** Shouldly
- **Naming:** `MethodNameScenarioExpectedResult` (no underscores)
- **Structure:** AAA pattern (Arrange, Act, Assert)
- **Cancellation:** Use `TestContext.Current.CancellationToken`

## Key Documentation

- [Wiki Home](wiki/Home.md) - Entry point
- [TypeCollections Guide](wiki/04-01-Overview.md) - Type system
- [TypeCollection Patterns](wiki/10-TypeCollection-Patterns.md) - ServiceTypeCollection vs MutableTypeCollection
- [Service Domains Guide](wiki/06-02-Creating-Service-Domain.md) - Service patterns
- [MessageLogging Guide](wiki/07-02-MessageLogging-Attribute.md) - Logging patterns
- [DataGateway Pattern](wiki/05-01-DataGateway-Pattern.md) - Data access
- [Developer's Guide](docs/DevelopersGuide.md) - Comprehensive guide
