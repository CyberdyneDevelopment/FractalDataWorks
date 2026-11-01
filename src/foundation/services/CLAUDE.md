# The services domain

The service base: `ServiceTypeBase` and the service and processor abstractions every domain service builds on, and `Fdw.Services`.

This folder, `public/src/foundation/services/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/services/`.

## Projects

fractaldataworks:

- `Fdw.Processors.Abstractions`
- `Fdw.Services`
- `Fdw.Services.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IGenericService` (Fdw.Services.Abstractions)
- `IDomainConfigurationProvider` (Fdw.Services.Abstractions)
- `ServiceTypeBase` (Fdw.Services.Abstractions)
- `IServiceMessage` (Fdw.Services.Abstractions)
- `IImplementationConfigurationProvider` (Fdw.Services.Abstractions)
- `IDomainServiceProvider` (Fdw.Services.Abstractions)
- `ServiceBase` (Fdw.Services.Abstractions)
- `ServiceTypeCollectionBase` (Fdw.Services.Abstractions)
- `IServiceType` (Fdw.Services.Abstractions)
- `IServiceConfiguration` (Fdw.Services.Abstractions)
- `IHealthState` (Fdw.Services.Abstractions)
- `IHealthMonitorService` (Fdw.Services.Abstractions)
- `IProcessor` (Fdw.Processors.Abstractions)
- `IImplementationServiceProvider` (Fdw.Services.Abstractions)
- `IServiceOption` (Fdw.Services.Abstractions)
- `IServiceTypeRegistration` (Fdw.Services.Abstractions)
- `IHealthMonitorProvider` (Fdw.Services.Abstractions)
- `IHealthMonitorImplementationConfiguration` (Fdw.Services.Abstractions)
- `HealthStateBase` (Fdw.Services.Abstractions)
- `IHealthCheckable` (Fdw.Services.Abstractions)
- and 22 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/foundation/services/Fdw.Processors.Abstractions.Tests/Fdw.Processors.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/services/Fdw.Services.Abstractions.Tests/Fdw.Services.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/services/Fdw.Services.Tests/Fdw.Services.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (6), `collections` (5), `results` (4), `commands` (3), `configuration` (3), `data` (2), `abstractions` (1), `connections` (1), `pipelines` (1).
Depended on by: `authentication` (18), `pipelines` (16), `connections` (12), `credentials` (11), `data` (10), `telemetry` (8), `endpoints` (7), `notifications` (6), `authorization` (5), `multitenancy` (5), `scheduling` (5), `quality` (4), `secretmanagers` (4), `ui` (4), `users` (4), `hosts` (3), `catalog` (2), `configuration` (2), `schema` (2), `shells` (2), `transformations` (2), `aegis` (1), `agents` (1), `calculations` (1), `datasets` (1), `registration` (1), `resiliency` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

A foundation project that references a domain outside the foundation is a layering defect or a misfiled project:

- `Fdw.Services` -> `Fdw.Commands.Data` (commands)
- `Fdw.Services` -> `Fdw.Commands.Data.Abstractions` (commands)
- `Fdw.Services` -> `Fdw.Commands.Data.Fluency` (commands)
- `Fdw.Services` -> `Fdw.Configuration` (configuration)
- `Fdw.Services` -> `Fdw.Configuration.SourceGenerators` (configuration)
- `Fdw.Services` -> `Fdw.Data.SourceGenerators` (data)
- `Fdw.Services` -> `Fdw.Etl.Abstractions` (pipelines)
- `Fdw.Services` -> `Fdw.Services.Connections.Abstractions` (connections)
- `Fdw.Services` -> `Fdw.Services.Data.Abstractions` (data)
- `Fdw.Services.Abstractions` -> `Fdw.Configuration.Abstractions` (configuration)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `principles`.
