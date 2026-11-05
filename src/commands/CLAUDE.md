# The commands domain

Commands: the command abstractions, data commands and their fluent builders, and development commands.

This folder, `public/src/commands/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/commands/`.

## Projects

fractaldataworks:

- `Fdw.Commands.Abstractions`
- `Fdw.Commands.Data`
- `Fdw.Commands.Data.Abstractions`
- `Fdw.Commands.Data.Fluency`
- `Fdw.Commands.Development.Abstractions`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IDataCommand` (Fdw.Commands.Data.Abstractions)
- `IQueryCommand` (Fdw.Commands.Data.Abstractions)
- `IDataCommandWithInput` (Fdw.Commands.Data.Abstractions)
- `IFilterableCommand` (Fdw.Commands.Data.Abstractions)
- `IDataCommandTranslator` (Fdw.Commands.Data.Abstractions)
- `DataCommandBase` (Fdw.Commands.Data.Abstractions)
- `IScriptedDataCommand` (Fdw.Commands.Data.Abstractions)
- `ITranslatorType` (Fdw.Commands.Abstractions)
- `IVectorizedDataCommand` (Fdw.Commands.Data.Abstractions)
- `DataCommandTranslatorBase` (Fdw.Commands.Data.Abstractions)
- `IGenericCommandType` (Fdw.Commands.Abstractions)
- `IJoinDefinition` (Fdw.Commands.Data.Abstractions)
- `DevelopmentCommandCategoryBase` (Fdw.Commands.Development.Abstractions)
- `IExpandableDataCommand` (Fdw.Commands.Data.Abstractions)
- `IFieldValueExtractor` (Fdw.Commands.Data.Abstractions)
- `ScriptedDataCommandBase` (Fdw.Commands.Data.Abstractions)
- `IConfigurationSaveCommand` (Fdw.Commands.Data.Abstractions)
- `VectorizedDataCommandBase` (Fdw.Commands.Data.Abstractions)
- `DevelopmentCommandTranslatorBase` (Fdw.Commands.Development.Abstractions)
- `ExecutionStrategyBase` (Fdw.Commands.Data.Abstractions)
- and 40 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/commands/Fdw.Commands.Abstractions.Tests/Fdw.Commands.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/commands/Fdw.Commands.Data.Abstractions.Tests/Fdw.Commands.Data.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/commands/Fdw.Commands.Data.Fluency.Tests/Fdw.Commands.Data.Fluency.Tests.csproj -c Release`
- `dotnet test public/tests/commands/Fdw.Commands.Data.Tests/Fdw.Commands.Data.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (10), `messagelogging` (8), `data` (7), `results` (4), `abstractions` (1), `pipelines` (1).
Depended on by: `connections` (26), `authentication` (11), `pipelines` (10), `data` (7), `credentials` (6), `calculations` (5), `notifications` (4), `telemetry` (4), `users` (4), `authorization` (3), `configuration` (3), `scheduling` (3), `services` (3), `agents` (2), `datasets` (2), `hosts` (2), `quality` (2), `catalog` (1), `endpoints` (1), `multitenancy` (1), `roslyn` (1), `schema` (1), `secretmanagers` (1), `ui` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `commands`, `builders`.
