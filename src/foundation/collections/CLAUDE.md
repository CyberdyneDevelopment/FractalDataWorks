# The collections domain

TypeCollections and state collections: the collection base types, their source generators, analyzers and code fixes.

This folder, `public/src/foundation/collections/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/collections/`.

## Projects

fractaldataworks:

- `Fdw.Collections`
- `Fdw.Collections.Analyzers` (not a package)
- `Fdw.Collections.SourceGenerators`
- `Fdw.StateCollections`
- `Fdw.StateCollections.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IStateOption` (Fdw.StateCollections.Abstractions)
- `IStateContext` (Fdw.StateCollections.Abstractions)
- `IStateMachine` (Fdw.StateCollections.Abstractions)
- `IStateTransition` (Fdw.StateCollections.Abstractions)
- `IStateTransitionHandler` (Fdw.StateCollections.Abstractions)
- `IStateMachineFactory` (Fdw.StateCollections.Abstractions)

## Rules that matter here

- A TypeCollection collects and dedupes its options; a new constructor parameter on an option base must be defaultable, or the generated not-found sentinel fails every host.
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

- `dotnet test public/tests/foundation/collections/Fdw.Collections.Analyzers.Tests/Fdw.Collections.Analyzers.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/collections/Fdw.Collections.SourceGenerators.Tests/Fdw.Collections.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/foundation/collections/Fdw.Collections.Tests/Fdw.Collections.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (3), `abstractions` (2), `results` (2).
Depended on by: `pipelines` (33), `connections` (32), `data` (24), `ui` (20), `authentication` (16), `transformations` (16), `calculations` (13), `credentials` (13), `commands` (10), `endpoints` (9), `notifications` (9), `resiliency` (9), `rowsources` (9), `workspaces` (9), `datasets` (8), `roslyn` (8), `schema` (8), `configuration` (7), `telemetry` (7), `mcp` (6), `multitenancy` (6), `aegis` (5), `authorization` (5), `hosts` (5), `quality` (5), `scheduling` (5), `secretmanagers` (5), `services` (5), `shells` (5), `users` (5), `agents` (4), `catalog` (4), `devsession` (4), `security` (4), `abstractions` (3), `results` (3), `documents` (2), `messagelogging` (2), `validation` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `collections`, `collections-domain`.
