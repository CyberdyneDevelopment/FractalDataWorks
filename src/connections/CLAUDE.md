# The connections domain

Connections: the connection service and its abstractions, the connection types (SQL Server, PostgreSQL, SQLite, HTTP, Roslyn workspace), their data dialects, SQL commands and DDL.

This folder, `public/src/connections/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/connections/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/connections/`.

## Projects

fractaldataworks:

- `Fdw.Data.Http`
- `Fdw.Data.MsSql`
- `Fdw.Data.OData`
- `Fdw.Data.OpenApi`
- `Fdw.Data.PostgreSql`
- `Fdw.Data.Sqlite`
- `Fdw.Schema.Ddl.MsSql`: SQL Server DDL generator for database schema generation
- `Fdw.Services.Connections`
- `Fdw.Services.Connections.Abstractions`
- `Fdw.Services.Connections.Clients`: API clients and DTOs for connection management operations.
- `Fdw.Services.Connections.Endpoints`: HTTP endpoints for the Connections domain, extracted from Fdw.Web.Api.
- `Fdw.Services.Connections.Http`
- `Fdw.Services.Connections.Http.Abstractions`
- `Fdw.Services.Connections.MsSql`
- `Fdw.Services.Connections.PostgreSql`
- `Fdw.Services.Connections.RoslynWorkspace`
- `Fdw.Services.Connections.RoslynWorkspace.Abstractions`
- `Fdw.Services.Connections.RoslynWorkspace.Registration`: The Roslyn workspace connection service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `Fdw.Services.Connections.Sql`
- `Fdw.Services.Connections.Sqlite`
- `Fdw.Sql.Commands`: SQL command handler + translator registry + DI extensions. Parallel to Fdw.Roslyn.Commands.
- `Fdw.Sql.Commands.Abstractions`: SQL command abstractions: ISqlCommand, ISqlCommandTranslator, SqlCommands / SqlCommandTranslators TypeCollections. Parallel to Fdw.Roslyn.Commands.Abstractions.
- `Fdw.Types.MsSql`

reference-servicetypes:

- `FdwReferenceConnections.FileSystem`
- `FdwReferenceConnections.FileSystem.Abstractions`
- `FdwReferenceConnections.FileSystem.Registration`: The file-system connection service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceConnections.Http`: Http connection — the aggregation over FDW's Http dialect, translators and components, plus its service type registration.
- `FdwReferenceConnections.Http.Registration`: The Http service-type registration: the [Implementation] and its configuration provider. References the aggregation package, so referencing THIS package brings the aggregation with it.
- `FdwReferenceConnections.MsSql`: MsSql connection — the aggregation over FDW's MsSql dialect, translators and components. The service-type registration lives in the sibling FdwReferenceConnections.MsSql.Registration package.
- `FdwReferenceConnections.MsSql.DataVault`: MsSql data vault — the sanctioned raw-ADO credential surface over an MsSql connection.
- `FdwReferenceConnections.MsSql.DataVault.Registration`: The MsSql credential-vault service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceConnections.MsSql.Registration`: The MsSql service-type registration: the [Implementation] and its configuration provider. References the aggregation package, so referencing THIS package brings the aggregation with it.
- `FdwReferenceConnections.PostgreSql`: PostgreSql connection — the aggregation over FDW's PostgreSql dialect, translators and components, plus its service type registration.
- `FdwReferenceConnections.PostgreSql.Registration`: The PostgreSql connection service-type registration: the [Implementation] and its configuration provider. References the aggregation package, so referencing THIS package brings the aggregation with it.
- `FdwReferenceConnections.Script`: The script extension of the database connections (FDW-1447): MsSqlScriptConnection, PostgreSqlScriptConnection and SqliteScriptConnection wrap their connections so a data engineer's SQL script, with named parameters, runs through the data gateway. Scripts are for ETL: only a host that references this package takes one.
- `FdwReferenceConnections.Sqlite`: Sqlite connection — the aggregation over FDW's Sqlite dialect, translators and components, plus its service type registration.
- `FdwReferenceConnections.Sqlite.Registration`: The Sqlite service-type registration: the [Implementation] and its configuration provider. References the aggregation package, so referencing THIS package brings the aggregation with it.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IConnectionImplementationConfiguration` (Fdw.Services.Connections.Abstractions)
- `IGenericConnection` (Fdw.Services.Connections.Abstractions)
- `IDataConnection` (Fdw.Services.Connections.Abstractions)
- `IConnectionType` (Fdw.Services.Connections.Abstractions)
- `IDatabasePath` (Fdw.Services.Connections.Abstractions)
- `SqlCommandTranslatorBase` (Fdw.Sql.Commands.Abstractions)
- `SqlCommandBase` (Fdw.Sql.Commands.Abstractions)
- `IConnectionProvider` (Fdw.Services.Connections.Abstractions)
- `ISqlDialect` (Fdw.Services.Connections.Abstractions)
- `IDataConnectionProvider` (Fdw.Services.Connections.Abstractions)
- `HttpResultCodeBase` (Fdw.Services.Connections.Http.Abstractions)
- `IConnectionConfigurationProvider` (Fdw.Services.Connections.Abstractions)
- `ISqlCommandTranslator` (Fdw.Sql.Commands.Abstractions)
- `IDocumentConnection` (Fdw.Services.Connections.Abstractions)
- `IConnectionFactory` (Fdw.Services.Connections.Abstractions)
- `IConnectionMetadata` (Fdw.Services.Connections.Abstractions)
- `IDataConnectionTransaction` (Fdw.Services.Connections.Abstractions)
- `IConnectionHealthService` (Fdw.Services.Connections.Abstractions)
- `ITransactionalDataConnection` (Fdw.Services.Connections.Abstractions)
- `IConnectionGrants` (Fdw.Services.Connections.Abstractions)
- and 65 more in the `.Abstractions` projects above

## Rules that matter here

- Connection-type-specific code (ADO.NET, dialect SQL, authentication types) belongs in the connection projects here and nowhere above them.
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

- `dotnet test public/tests/connections/Fdw.Data.MsSql.Tests/Fdw.Data.MsSql.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Data.OData.Tests/Fdw.Data.OData.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Schema.Ddl.MsSql.Tests/Fdw.Schema.Ddl.MsSql.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Abstractions.Tests/Fdw.Services.Connections.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Clients.Tests/Fdw.Services.Connections.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Http.Abstractions.Tests/Fdw.Services.Connections.Http.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Http.Tests/Fdw.Services.Connections.Http.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Limits.Tests/Fdw.Services.Connections.Limits.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.PostgreSql.Tests/Fdw.Services.Connections.PostgreSql.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.RoslynWorkspace.Abstractions.Tests/Fdw.Services.Connections.RoslynWorkspace.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.RoslynWorkspace.Tests/Fdw.Services.Connections.RoslynWorkspace.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Sql.Tests/Fdw.Services.Connections.Sql.Tests.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.TestDouble/Fdw.Services.Connections.TestDouble.csproj -c Release`
- `dotnet test public/tests/connections/Fdw.Services.Connections.Tests/Fdw.Services.Connections.Tests.csproj -c Release`
- `dotnet test public/tests/connections/FdwReferenceConnections.Data.Tests/FdwReferenceConnections.Data.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.FileSystem.Abstractions.Tests/FdwReferenceConnections.FileSystem.Abstractions.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.FileSystem.Tests/FdwReferenceConnections.FileSystem.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.Http.Tests/FdwReferenceConnections.Http.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.MsSql.Tests/FdwReferenceConnections.MsSql.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.Script.Tests/FdwReferenceConnections.Script.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/connections/FdwReferenceConnections.Sqlite.Tests/FdwReferenceConnections.Sqlite.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `data` (52), `messagelogging` (41), `collections` (32), `results` (32), `commands` (26), `configuration` (19), `services` (12), `secretmanagers` (9), `abstractions` (7), `schema` (7), `authentication` (6), `datasets` (6), `pipelines` (6), `rowsources` (6), `endpoints` (4), `authorization` (3), `roslyn` (3), `validation` (3), `scheduling` (2), `users` (2), `calculations` (1), `catalog` (1), `documents` (1), `hosts` (1), `multitenancy` (1), `quality` (1), `registration` (1), `security` (1), `ui` (1).
Depended on by: `data` (9), `pipelines` (7), `endpoints` (5), `credentials` (4), `authentication` (3), `authorization` (3), `datasets` (3), `catalog` (2), `multitenancy` (2), `notifications` (2), `quality` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `ui` (2), `users` (2), `aegis` (1), `services` (1), `shells` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `connections`, `axes`, `gateways`, `catalog-conn`.
