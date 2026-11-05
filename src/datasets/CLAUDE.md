# The datasets domain

Data sets and data stores: data set types and the REST and SQL Server data stores.

This folder, `public/src/datasets/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/datasets/`.

## Projects

fractaldataworks:

- `Fdw.Data.DataSets`
- `Fdw.Data.DataSets.Abstractions`
- `Fdw.Data.DataStores.Rest`
- `Fdw.Data.DataStores.SqlServer`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IDataField` (Fdw.Data.DataSets.Abstractions)
- `IDataQuery` (Fdw.Data.DataSets.Abstractions)
- `IDataSetType` (Fdw.Data.DataSets.Abstractions)
- `StandInStrategyBase` (Fdw.Data.DataSets.Abstractions)
- `DataSetTypeBase` (Fdw.Data.DataSets.Abstractions)
- `IDataSetExecutionContext` (Fdw.Data.DataSets.Abstractions)
- `DataSetSourceMapperTypeBase` (Fdw.Data.DataSets.Abstractions)
- `IDataSetSourceMapperType` (Fdw.Data.DataSets.Abstractions)
- `IStandInStrategy` (Fdw.Data.DataSets.Abstractions)
- `IDataSetCatalog` (Fdw.Data.DataSets.Abstractions)
- `IQueryExpression` (Fdw.Data.DataSets.Abstractions)

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

- `dotnet test public/tests/datasets/Fdw.Data.DataSets.Abstractions.Tests/Fdw.Data.DataSets.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/datasets/Fdw.Data.DataSets.Tests/Fdw.Data.DataSets.Tests.csproj -c Release`
- `dotnet test public/tests/datasets/Fdw.Data.DataStores.Rest.Tests/Fdw.Data.DataStores.Rest.Tests.csproj -c Release`
- `dotnet test public/tests/datasets/Fdw.Data.DataStores.SqlServer.Tests/Fdw.Data.DataStores.SqlServer.Tests.csproj -c Release`
- `dotnet test public/tests/datasets/Fdw.Data.DataStores.Tests/Fdw.Data.DataStores.Tests.csproj -c Release`

## Dependencies

Depends on: `data` (15), `messagelogging` (9), `collections` (8), `transformations` (7), `configuration` (6), `results` (4), `connections` (3), `commands` (2), `schema` (2), `abstractions` (1), `calculations` (1), `services` (1), `workspaces` (1).
Depended on by: `data` (9), `connections` (6), `pipelines` (3), `authentication` (2), `authorization` (2), `catalog` (2), `endpoints` (2), `multitenancy` (2), `quality` (2), `scheduling` (2), `schema` (2), `ui` (2), `users` (2), `workspaces` (2), `calculations` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `datasets`, `datastores`.
