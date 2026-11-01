# The abstractions domain

The base contracts every other domain builds on: the core FDW abstractions (`IGenericResult` among them) and the shared type abstractions.

This folder, `public/src/foundation/abstractions/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/abstractions/`.

## Projects

fractaldataworks:

- `Fdw.Abstractions`
- `Fdw.Types.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IGenericResult` (Fdw.Abstractions)
- `IGenericMessage` (Fdw.Abstractions)
- `IGenericConfiguration` (Fdw.Abstractions)
- `IGenericService` (Fdw.Abstractions)
- `IGenericCommand` (Fdw.Abstractions)
- `IImplementationConfiguration` (Fdw.Abstractions)
- `IServiceFactory` (Fdw.Abstractions)
- `IDomainConfiguration` (Fdw.Abstractions)
- `IPlatformService` (Fdw.Abstractions)
- `IPagedResponse` (Fdw.Abstractions)
- `IClientMessage` (Fdw.Abstractions)
- `CollectionKindBase` (Fdw.Types.Abstractions)
- `ICollectionKind` (Fdw.Types.Abstractions)
- `IAsyncServiceFactory` (Fdw.Abstractions)
- `ITypesProvider` (Fdw.Types.Abstractions)
- `IMemoryStore` (Fdw.Abstractions)
- `IPagedRequest` (Fdw.Abstractions)
- `IServiceDispatchHost` (Fdw.Abstractions)

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

- `dotnet test public/tests/foundation/abstractions/Fdw.Abstractions.Tests/Fdw.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/abstractions/Fdw.Types.Abstractions.Tests/Fdw.Types.Abstractions.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (3), `results` (1).
Depended on by: `connections` (7), `data` (6), `agents` (4), `authentication` (4), `credentials` (4), `pipelines` (4), `ui` (4), `configuration` (3), `messagelogging` (3), `shells` (3), `users` (3), `workspaces` (3), `aegis` (2), `calculations` (2), `collections` (2), `endpoints` (2), `multitenancy` (2), `notifications` (2), `resiliency` (2), `schema` (2), `security` (2), `telemetry` (2), `authorization` (1), `catalog` (1), `commands` (1), `datasets` (1), `devsession` (1), `documents` (1), `hosts` (1), `mcp` (1), `registration` (1), `results` (1), `roslyn` (1), `rowsources` (1), `scheduling` (1), `services` (1), `transformations` (1), `validation` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `principles`, `composition`.
