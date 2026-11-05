# The schema domain

Schema: schema abstractions, DDL generation and its MSBuild tasks, schema clients and endpoints, JSON schema.

This folder, `public/src/schema/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/schema/`.

## Projects

fractaldataworks:

- `Fdw.Data.JsonSchema`
- `Fdw.Schema.Abstractions`: Schema abstractions for Fdw including PropertyRole type system
- `Fdw.Schema.Clients`: API clients and DTOs for schema operations.
- `Fdw.Schema.Clients.Abstractions`: DTO models for schema API client operations.
- `Fdw.Schema.Ddl`: DDL command interfaces and definitions for generating database schema scripts
- `Fdw.Schema.Ddl.Tasks` (not a package)
- `Fdw.Schema.Endpoints`: HTTP endpoints for the Schema domain, extracted from Fdw.Web.Api.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IPropertyDefinition` (Fdw.Schema.Abstractions)
- `IDatumType` (Fdw.Schema.Abstractions)
- `ISchemaDefinition` (Fdw.Schema.Abstractions)
- `IKeyDefinition` (Fdw.Schema.Abstractions)
- `DatumTypeBase` (Fdw.Schema.Abstractions)
- `IIndexDefinition` (Fdw.Schema.Abstractions)
- `DataLayoutBase` (Fdw.Schema.Abstractions)
- `AxisTypeBase` (Fdw.Schema.Abstractions)
- `IDataLayout` (Fdw.Schema.Abstractions)
- `ISchemaProvider` (Fdw.Schema.Clients.Abstractions)
- `IColumnDefinition` (Fdw.Schema.Abstractions)
- `IFieldDefinition` (Fdw.Schema.Abstractions)
- `IAxisType` (Fdw.Schema.Abstractions)
- `SchemaDefinitionBase` (Fdw.Schema.Abstractions)

## Rules that matter here

- No fallbacks: a `??` never falls back to a value that could be a real one, and a missing value fails with a MessageLogging error and a failed result. Allowed: a null logger, `?? string.Empty`, `?? []`, `??=` on a field or local, a chain of failure results or messages, a `[PlaceholderText]` constant, a `[PlatformDefault]` member, and a member Mike approved with `[ApprovedFallback]`. Never a default message. FDW055 enforces it.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/schema/Fdw.Data.JsonSchema.Tests/Fdw.Data.JsonSchema.Tests.csproj -c Release`
- `dotnet test public/tests/schema/Fdw.Schema.Abstractions.Tests/Fdw.Schema.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/schema/Fdw.Schema.Clients.Tests/Fdw.Schema.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/schema/Fdw.Schema.Ddl.Tests/Fdw.Schema.Ddl.Tests.csproj -c Release`
- `dotnet test public/tests/schema/Fdw.Schema.Tests/Fdw.Schema.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (8), `data` (8), `messagelogging` (6), `pipelines` (6), `endpoints` (5), `authentication` (3), `results` (3), `abstractions` (2), `authorization` (2), `connections` (2), `datasets` (2), `scheduling` (2), `secretmanagers` (2), `services` (2), `users` (2), `calculations` (1), `catalog` (1), `commands` (1), `configuration` (1), `hosts` (1), `multitenancy` (1), `quality` (1), `registration` (1), `security` (1), `ui` (1), `validation` (1).
Depended on by: `connections` (7), `data` (5), `endpoints` (3), `authentication` (2), `authorization` (2), `catalog` (2), `datasets` (2), `multitenancy` (2), `pipelines` (2), `quality` (2), `scheduling` (2), `ui` (2), `users` (2).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.
