# Domain: Schema

## Purpose

The Schema domain provides **database schema discovery and DDL generation** that is connection-type agnostic. It discovers tables, columns, indexes, and constraints from any supported database, and can generate DDL scripts for schema management.

## Projects

| Project | Purpose |
|---------|---------|
| `Schema.Abstractions` | Schema model interfaces — `IColumnDefinition`, `IIndexDefinition`, `IPropertyDefinition`, plus role/layout/key abstractions under `Roles/`, `Layouts/`, `Keys/` |
| `Schema.Clients` | Schema HTTP client for remote discovery |
| `Schema.Clients.Abstractions` | `ISchemaClient` interface |
| `Schema.Ddl` | DDL generation engine (connection-type agnostic) |
| `Schema.Ddl.MsSql` | SQL Server DDL dialect implementation |
| `Schema.Ddl.Tasks` | DDL task runner (apply, diff, migrate) |
| `Schema.Endpoints` | Schema discovery API endpoints |

## Key Types

- **`IColumnDefinition`** / **`IIndexDefinition`** / **`IPropertyDefinition`** -- Schema model interfaces representing discovered database objects (in `Fdw.Schema.Abstractions`).
- **`Schema.Clients` HTTP client** -- Remote schema discovery against a schema-discovery API endpoint.
- **DDL generators** -- Produce CREATE/ALTER/DROP statements from schema models.
- **Schema import persister** -- Saves discovered schema to configuration database for auto-persist.

## Patterns

### Discovery Pattern

Schema discovery queries database metadata catalogs (e.g., `INFORMATION_SCHEMA`, `sys.columns`) through the command framework. The discovery process is connection-type agnostic at the interface level -- each database implementation provides its own metadata queries.

```
Schema discovery API endpoint
  -> Connection provider resolves the connection
  -> Connection-specific schema commands query metadata
  -> Results mapped to IColumnDefinition / IIndexDefinition / IPropertyDefinition models
  -> Optionally persisted to configuration database
```

### Auto-Persist Pattern

Discovered schema can be automatically persisted to the configuration database for caching and offline access. This avoids repeated metadata queries against production databases.

### Connection-Type Agnostic DDL

The DDL domain separates the generation engine from dialect implementations:

```
Schema model (ITableSchema)
  -> DDL engine (Schema.Ddl) -- generates abstract operations
  -> Dialect (Schema.Ddl.MsSql) -- renders to SQL Server syntax
```

Adding a new database dialect means implementing the rendering layer only. The generation engine and schema model remain unchanged.

### DDL Tasks

`Schema.Ddl.Tasks` provides executable tasks for schema management:
- **Apply** -- Execute DDL against a target database
- **Diff** -- Compare two schemas and generate migration DDL
- **Migrate** -- Apply incremental changes

## Rules

1. **Schema discovery is connection-type agnostic.** Consumer code works with `IColumnDefinition` / `IIndexDefinition` / `IPropertyDefinition` interfaces, never database-specific metadata types.
2. **No connection-type checking.** Do not inspect the connection type to decide which metadata queries to run. The connection's command translator handles dialect differences.
3. **DDL generation is two-layered.** Engine produces abstract operations; dialect renders to SQL. Never mix SQL syntax into the engine layer.
4. **Schema models are read-only.** Discovery produces immutable schema objects. Modifications go through DDL generation, not schema mutation.
5. **Persisted schema is a cache.** Auto-persist stores a snapshot. Always re-discover when accuracy matters.

## Related Domains

- **Commands** -- Schema discovery uses data commands to query metadata catalogs
- **Services.Connections** -- Discovery runs against connections resolved by the provider
- **Configuration** -- `[ManagedConfiguration]` generates DDL templates via this domain
- **UI.Components** -- Schema browser component consumes the Schema HTTP client
- **Data** -- Schema models inform DataStore and DataSet field definitions
