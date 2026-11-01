# The calculations domain

Calculations: calculation kinds, aggregations, their services, endpoints, SignalR hub and clients.

This folder, `public/src/calculations/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/calculations/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/calculations/`.

## Projects

fractaldataworks:

- `Fdw.Calculations`
- `Fdw.Calculations.Abstractions`
- `Fdw.Calculations.Aggregations`
- `Fdw.Calculations.Endpoints`: Generic endpoint base classes and DTOs for calculation operations via FastEndpoints.
- `Fdw.Calculations.SignalR`
- `Fdw.Services.Calculations`
- `Fdw.Services.Calculations.Abstractions`
- `Fdw.Web.Calculations.Clients`: API clients and DTOs for calculation and formula operations.
- `Fdw.Web.Calculations.Clients.Abstractions`: DTO models for calculation and formula API client operations.

reference-servicetypes:

- `FdwReferenceCalculations.Endpoints`: The Calculations domain's HTTP endpoints, the collections that declare them, and the API service type that drives their registration.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ICalculationEntity` (Fdw.Services.Calculations.Abstractions)
- `ICalculationContext` (Fdw.Calculations.Abstractions)
- `CalculationTreeResultCodeBase` (Fdw.Services.Calculations.Abstractions)
- `CalculationOperationBase` (Fdw.Services.Calculations.Abstractions)
- `ICompiledCalculation` (Fdw.Services.Calculations.Abstractions)
- `ICalculationEntityService` (Fdw.Services.Calculations.Abstractions)
- `CalculationResultCodeBase` (Fdw.Calculations.Abstractions)
- `CalculationNodeBase` (Fdw.Services.Calculations.Abstractions)
- `ICalculationValueKind` (Fdw.Services.Calculations.Abstractions)
- `WindowedCalculationTypeBase` (Fdw.Calculations.Abstractions)
- `ICompiledCalculationSet` (Fdw.Services.Calculations.Abstractions)
- `CalculationTypeBase` (Fdw.Calculations.Abstractions)
- `PeriodComparisonTypeBase` (Fdw.Calculations.Abstractions)
- `CalculationEntityResultCodeBase` (Fdw.Services.Calculations.Abstractions)
- `ICalculationType` (Fdw.Calculations.Abstractions)
- `ICalculationInputResolver` (Fdw.Services.Calculations.Abstractions)
- `ScalarValueTypeBase` (Fdw.Services.Calculations.Abstractions)
- `CalculationEntityTypeBase` (Fdw.Services.Calculations.Abstractions)
- `CalculationSourceTypeBase` (Fdw.Services.Calculations.Abstractions)
- `OperationParameterKindBase` (Fdw.Services.Calculations.Abstractions)
- and 39 more in the `.Abstractions` projects above

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/calculations/Fdw.Services.Calculations.Tests/Fdw.Services.Calculations.Tests.csproj -c Release`
- `dotnet test public/tests/calculations/Fdw.Web.Calculations.Clients.Tests/Fdw.Web.Calculations.Clients.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (13), `data` (12), `messagelogging` (11), `results` (7), `commands` (5), `endpoints` (4), `configuration` (3), `abstractions` (2), `authentication` (2), `validation` (2), `workspaces` (2), `authorization` (1), `datasets` (1), `hosts` (1), `multitenancy` (1), `pipelines` (1), `services` (1), `transformations` (1).
Depended on by: `transformations` (7), `pipelines` (4), `catalog` (2), `authentication` (1), `authorization` (1), `connections` (1), `data` (1), `datasets` (1), `endpoints` (1), `multitenancy` (1), `quality` (1), `scheduling` (1), `schema` (1), `ui` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-calc`.
