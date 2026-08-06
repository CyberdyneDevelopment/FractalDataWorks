# Domain: Configuration

## Purpose

The Configuration domain provides **database-backed configuration** that integrates with .NET's `IOptions<T>` / `IOptionsMonitor<T>` system. Configuration is read from SQL Server tables at startup, flattened into `IConfiguration` keys, and bound to strongly-typed classes via the standard .NET options pattern. Writes go through `IConfigurationWriter<T>`.

## Projects

| Project | Purpose |
|---------|---------|
| `Configuration` | Core: `ManagedConfigurationAttribute`, config source integration |
| `Configuration.Abstractions` | Interfaces for configuration types |
| `Configuration.Endpoints` | API endpoints for configuration management |
| `Configuration.MsSql` | `MsSqlConfigurationSource` -- reads config tables into IConfiguration |
| `Configuration.SourceGenerators` | Generates boilerplate from `[ManagedConfiguration]` attribute |
| `Configuration.UI.SourceGenerators` | Generates UI editor components for configuration classes |
| `Configuration.Writers` | `IConfigurationWriter<T>` base implementation |
| `Configuration.Writers.Abstractions` | Writer interfaces |
| `Configuration.Writers.InMemory` | In-memory writer for testing |
| `Configuration.Writers.MsSql` | SQL Server writer (INSERT new version row) |

## Key Types

- **`[ManagedConfiguration]`** -- Source generator attribute. Placed on concrete configuration classes. Generates SQL DDL templates, IOptions binding, and UI editors.
- **`MsSqlConfigurationSource`** -- Reads ALL `[ManagedConfiguration]` types from SQL Server at startup. Queries `WHERE IsCurrent=1 AND IsDeleted=0`, flattens parent-child relationships into IConfiguration key paths.
- **`IConfigurationWriter<T>`** -- Write interface. Inserts a new version row (version-on-write pattern), marks old row as `IsCurrent=0`.
- **`ServiceConfigurationMonitor<T>`** -- Bridges `IOptionsMonitor<T>` change notifications to service domain reconfiguration.

## Patterns

### ManagedConfiguration Attribute

```csharp
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "MsSql")]
public partial class MsSqlConnectionConfiguration : IConnectionConfiguration
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }   // FK to conn.Connection.Id
    public string Server { get; set; }
    public string Database { get; set; }
    public int Port { get; set; }
}
```

The attribute carries `ServiceCategory` / `ServiceType` plus optional
`DisplayName`, `Description`, `GenerateDdl`, `GenerateValidator`,
`GenerateUi`, `OnDelete`, `DatabaseProvider`. The physical schema/table
names come from the corresponding `DataContainer` registration, not from
the attribute.

**Generated artifacts:**
- SQL DDL template for the table
- IConfiguration key path: `Connections:MsSql:{index}:{PropertyName}`
- IOptions binding registration

### Configuration Read Pipeline

```
ConfigurationDb tables (conn / data / pipe / sched / quality / …)
  -> MsSqlConfigurationSource queries WHERE IsCurrent=1 AND IsDeleted=0
  -> Flattens parent-child into IConfiguration keys
  -> IOptions<T> / IOptionsMonitor<T> binding
  -> Three-phase registration reads bound options
```

### Configuration Write Pipeline

```
API endpoint receives update DTO
  -> IConfigurationWriter<T>.Write(config)
  -> INSERT new row with IsCurrent=1
  -> UPDATE old row SET IsCurrent=0
  -> IOptionsMonitor<T> fires change notification
  -> ServiceConfigurationMonitor reconfigures service
```

### Parent-Child Flattening

Configuration tables use parent-child relationships. A `Connection` table has child tables like `MsSqlConnection`, `PostgreSqlConnection`. The config source flattens these into:

```
Connections:MsSql:0:Name = "ProductionDb"
Connections:MsSql:0:Server = "localhost"
Connections:MsSql:0:Database = "ConfigurationDb"
Connections:PostgreSql:0:Name = "AnalyticsDb"
```

The key path is `{ServiceCategory}s:{ServiceType}:{index}:{PropertyName}`.

## Rules

1. **`[ManagedConfiguration]` only on concrete classes.** Not on generic base classes -- the source generator cannot process generic types.
2. **Properties use `{ get; set; }`.** IOptions binding requires mutable setters. Never use `{ get; init; }`.
3. **Property names match SQL column names exactly.** Case-sensitive. The config source maps by name.
4. **Child tables use `{ParentTableName}Id` FK.** The flattening logic depends on this convention.
5. **No JSON columns.** Use relational child tables for nested data.
6. **Never create `*ConfigurationService` classes.** Use `IOptionsMonitor<T>` + `ServiceConfigurationMonitor<T>` for reactive configuration.
7. **Never create custom configuration loaders.** Use the three-phase Configure/Register/Initialize pattern.
8. **Never bypass `IOptionsMonitor<T>`.** Do not cache configuration values manually. Let the options system handle change notifications.
9. **Required configuration must fail-fast.** If a required config section is missing, log with `Critical` and `return 1`. Never fall back to `?? new T()`.

## Debugging Configuration Issues

If configuration is not loading, check in this order:

1. `[ManagedConfiguration]` attribute present on the concrete class
2. C# property names match SQL column names exactly
3. SQL rows have `IsCurrent = 1 AND IsDeleted = 0`
4. `ServiceCategory` and `ServiceType` in the attribute match the table data
5. The configuration source is registered in hosting (via `AddFrameworkConfigurationDb`)

## Related Domains

- **Services.*** -- All service domains consume configuration via `IOptions<T>`
- **Hosting** -- `AddFrameworkConfigurationDb` registers the config source
- **Commands** -- Config writers use data commands internally
- **Schema** -- DDL templates generated from `[ManagedConfiguration]`
