# The data domain

Data: the data abstractions and DataGateway, data services and endpoints, data containers, nodes, paths, builders, execution, serialization and the data source generators.

This folder, `public/src/data/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/data/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/data/`.

## Projects

fractaldataworks:

- `Fdw.Data`
- `Fdw.Data.Abstractions`
- `Fdw.Data.Builders`
- `Fdw.Data.Configuration`
- `Fdw.Data.DataContainers.Abstractions`
- `Fdw.Data.DataNodes`
- `Fdw.Data.DataPaths`
- `Fdw.Data.Files`
- `Fdw.Data.Importers.Abstractions`
- `Fdw.Data.Options.Abstractions`: Data options, abstractions (FDW-5344): the `DataOptions` root collection of option types, the context and outcome of applying an option, its `DATAOPTION` result codes and the fields of a stored option row.
- `Fdw.Data.Options`: Data options (FDW-5344): the `SelectList` option type of `DataOptions`, with `Include` and `Exclude`. The `ReadDataOptions` pipeline step (`Fdw.Services.Etl`) reads the stored rows and keeps the resulting list in a variable.
- `Fdw.Data.Serialization`
- `Fdw.Data.SourceGenerators`: Incremental source generator that creates reflection-free POCO mappers from [GenerateMapper] attributes.
- `Fdw.Services.Data`
- `Fdw.Services.Data.Abstractions`
- `Fdw.Services.Data.Clients`: API clients and DTOs for DataStore and DataSet management operations.
- `Fdw.Services.Data.Clients.Models`: DTO models for data API client operations.
- `Fdw.Services.Data.Endpoints`: HTTP endpoints for the Data domain, extracted from Fdw.Web.Api.
- `Fdw.Services.Data.Etl`: The ETL server's data gateway (FDW-1952): the data gateway, copied so it can be tuned for pipelines on its own, with the ability to read a declared container at another physical path and name. Only the ETL host references it.
- `Fdw.Services.Data.Etl.Registration`: The ETL data gateway registration (FDW-1952): the [Implementation] only. Referencing THIS package brings the ETL data gateway and registers it in place of the standard data gateway factory; the ETL host references it and the API host does not.
- `Fdw.Services.Data.SignalR`
- `Fdw.Services.Execution.Abstractions`

reference-servicetypes:

- `FdwReferenceData.Endpoints`: The Data domain HTTP endpoints - connections, datasets, datastores, schema, field mappings and DDL.
- `FdwReferenceData.FileSystem`
- `FdwReferenceDataPaths.Templates`: Worked DataPathTemplate examples — tenant-scoped path templates joining FDW's DataPathTemplates collection.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IConfigurationGatewayProvider` (Fdw.Services.Data.Abstractions)
- `IStorageContainer` (Fdw.Data.Abstractions)
- `IDataContainer` (Fdw.Data.Abstractions)
- `IField` (Fdw.Data.Abstractions)
- `IConfigurationGateway` (Fdw.Services.Data.Abstractions)
- `IDataGateway` (Fdw.Services.Data.Abstractions)
- `IDataStore` (Fdw.Data.Abstractions)
- `IFilterExpression` (Fdw.Data.Abstractions)
- `IDataField` (Fdw.Data.Abstractions)
- `IFilterNode` (Fdw.Data.Abstractions)
- `IDataRow` (Fdw.Data.DataContainers.Abstractions)
- `IDataNodePath` (Fdw.Data.Abstractions)
- `IDataGatewayProvider` (Fdw.Services.Data.Abstractions)
- `IDataNode` (Fdw.Data.Abstractions)
- `DataTypeConverterBase` (Fdw.Data.Abstractions)
- `IOrderingExpression` (Fdw.Data.Abstractions)
- `IContainerSchema` (Fdw.Data.Abstractions)
- `IDataPath` (Fdw.Data.Abstractions)
- `IDataTypeConverter` (Fdw.Data.Abstractions)
- `IContainerKey` (Fdw.Data.Abstractions)
- and 139 more in the `.Abstractions` projects above

## Rules that matter here

- `IDataGateway` is the only data path. Containers abstract physical storage: never pass raw schema and table strings above the connection layer.
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

- `dotnet test public/tests/data/Fdw.Data.Abstractions.Tests/Fdw.Data.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.Builders.Tests/Fdw.Data.Builders.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.DataContainers.Abstractions.Tests/Fdw.Data.DataContainers.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.DataNodes.Tests/Fdw.Data.DataNodes.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.Importers.Abstractions.Tests/Fdw.Data.Importers.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.SourceGenerators.Tests/Fdw.Data.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Data.Tests/Fdw.Data.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Data.Abstractions.Tests/Fdw.Services.Data.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Data.Clients.Tests/Fdw.Services.Data.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Data.Endpoints.Tests/Fdw.Services.Data.Endpoints.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Data.Etl.Tests/Fdw.Services.Data.Etl.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Data.Tests/Fdw.Services.Data.Tests.csproj -c Release`
- `dotnet test public/tests/data/Fdw.Services.Execution.Abstractions.Tests/Fdw.Services.Execution.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/data/FdwReferenceData.Endpoints.Tests/FdwReferenceData.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/data/FdwReferenceDataPaths.Templates.Tests/FdwReferenceDataPaths.Templates.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `collections` (24), `messagelogging` (16), `results` (12), `services` (10), `configuration` (9), `connections` (9), `datasets` (9), `pipelines` (9), `commands` (7), `abstractions` (6), `endpoints` (6), `authentication` (5), `schema` (5), `secretmanagers` (5), `hosts` (4), `rowsources` (3), `aegis` (2), `authorization` (2), `scheduling` (2), `users` (2), `workspaces` (2), `calculations` (1), `catalog` (1), `multitenancy` (1), `quality` (1), `registration` (1), `security` (1), `ui` (1), `validation` (1).
Depended on by: `connections` (52), `pipelines` (35), `authentication` (22), `datasets` (15), `credentials` (14), `calculations` (12), `telemetry` (10), `notifications` (9), `scheduling` (9), `transformations` (9), `workspaces` (9), `configuration` (8), `schema` (8), `users` (8), `authorization` (7), `commands` (7), `endpoints` (7), `hosts` (7), `multitenancy` (6), `quality` (6), `ui` (6), `agents` (5), `catalog` (4), `aegis` (3), `resiliency` (3), `secretmanagers` (3), `rowsources` (2), `services` (2), `shells` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `gateways`, `catalog-data`.
