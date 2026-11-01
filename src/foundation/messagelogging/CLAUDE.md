# The messagelogging domain

MessageLogging: messages, the MessageLogging abstractions and their source generators.

This folder, `public/src/foundation/messagelogging/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/messagelogging/`.

## Projects

fractaldataworks:

- `Fdw.MessageLogging.Abstractions`
- `Fdw.MessageLogging.SourceGenerators`
- `Fdw.Messages`

## Seams

This domain has no `.Abstractions` project; its projects above are its surface.

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

- `dotnet test public/tests/foundation/messagelogging/Fdw.MessageLogging.Abstractions.Tests/Fdw.MessageLogging.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/messagelogging/Fdw.MessageLogging.SourceGenerators.Tests/Fdw.MessageLogging.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/messagelogging/Fdw.Messages.Tests/Fdw.Messages.Tests.csproj -c Release`

## Dependencies

Depends on: `abstractions` (3), `collections` (2).
Depended on by: `connections` (41), `pipelines` (29), `authentication` (28), `transformations` (21), `data` (16), `credentials` (15), `endpoints` (13), `notifications` (12), `calculations` (11), `mcp` (11), `resiliency` (11), `roslyn` (11), `configuration` (10), `telemetry` (10), `ui` (10), `datasets` (9), `workspaces` (9), `authorization` (8), `commands` (8), `multitenancy` (8), `agents` (7), `hosts` (7), `scheduling` (7), `quality` (6), `schema` (6), `services` (6), `secretmanagers` (5), `shells` (5), `users` (5), `aegis` (3), `catalog` (3), `collections` (3), `devsession` (3), `documents` (3), `rowsources` (3), `security` (2), `results` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.
