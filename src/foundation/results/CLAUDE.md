# The results domain

Results: `GenericResult`, the result every operation returns, and the result codes, severities, categories and statuses it carries. The `IGenericResult` interface is in `abstractions`.

This folder, `public/src/foundation/results/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/results/`.

## Projects

fractaldataworks:

- `Fdw.Results`
- `Fdw.Results.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IResultCode` (Fdw.Results.Abstractions)
- `IResultSeverity` (Fdw.Results.Abstractions)
- `IResultDetails` (Fdw.Results.Abstractions)
- `ResultCodeBase` (Fdw.Results.Abstractions)
- `ResultCategoryBase` (Fdw.Results.Abstractions)
- `ResultSeverityBase` (Fdw.Results.Abstractions)
- `ResultStatusBase` (Fdw.Results.Abstractions)
- `IResultStatus` (Fdw.Results.Abstractions)
- `IResultCategory` (Fdw.Results.Abstractions)

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

- `dotnet test public/tests/foundation/results/Fdw.Results.Abstractions.Tests/Fdw.Results.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/results/Fdw.Results.Tests/Fdw.Results.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (3), `abstractions` (1), `messagelogging` (1).
Depended on by: `connections` (32), `pipelines` (13), `data` (12), `authentication` (11), `roslyn` (9), `transformations` (8), `agents` (7), `calculations` (7), `credentials` (7), `notifications` (7), `workspaces` (6), `endpoints` (5), `mcp` (5), `users` (5), `commands` (4), `configuration` (4), `datasets` (4), `quality` (4), `services` (4), `shells` (4), `telemetry` (4), `ui` (4), `authorization` (3), `hosts` (3), `multitenancy` (3), `resiliency` (3), `scheduling` (3), `schema` (3), `aegis` (2), `collections` (2), `devsession` (2), `documents` (2), `rowsources` (2), `abstractions` (1), `catalog` (1), `secretmanagers` (1), `security` (1), `validation` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `results`.
