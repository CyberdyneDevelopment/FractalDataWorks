# The configuration domain

Configuration: the configuration abstractions, managed configuration and its source generators, the settings service and the configuration endpoints.

This folder, `public/src/configuration/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/configuration/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/configuration/`.

## Projects

fractaldataworks:

- `Fdw.Configuration`: Generic multi-tenant hierarchical configuration provider for Microsoft.Extensions.Configuration
- `Fdw.Configuration.Abstractions`: Abstractions for configuration options loading patterns (IOptions, IOptionsSnapshot, IOptionsMonitor)
- `Fdw.Configuration.Endpoints`: Generic endpoint base classes and DTOs for managing configuration instances via FastEndpoints.
- `Fdw.Configuration.SourceGenerators`: Incremental source generator that creates DDL definitions, ConfigurationTypes collection, FluentValidation validators, and validation logging from [ManagedConfiguration] attributes.
- `Fdw.Configuration.UI.SourceGenerators`: Incremental source generator that creates Web and TUI components from [PersistConfiguration] attributes.
- `Fdw.Services.Settings`
- `Fdw.Services.Settings.Clients`: API client, DTOs, and registration types for server settings operations.
- `Fdw.Services.Settings.Endpoints`: Generic endpoint base classes and DTOs for managing layered settings via FastEndpoints.

reference-servicetypes:

- `FdwReferenceConfiguration.Endpoints`: The Configuration domain HTTP endpoints - configuration instances, categories, types, settings and secret managers.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ForeignKeyActionBase` (Fdw.Configuration.Abstractions)
- `IForeignKeyAction` (Fdw.Configuration.Abstractions)
- `ConfigurationBase` (Fdw.Configuration.Abstractions)
- `EnvironmentTypeBase` (Fdw.Configuration.Abstractions)
- `IConfigurationDdlProvider` (Fdw.Configuration.Abstractions)
- `IEnvironmentType` (Fdw.Configuration.Abstractions)

## Rules that matter here

- Top-level configurations are written through their domain provider's `Save()`/`Delete()`; `ConfigurationSaveCommand<T>` only for child records. A provider with a parent names the key type it joins on (Logical or Physical); never a default.
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

- `dotnet test public/tests/configuration/Fdw.Configuration.SourceGenerators.Tests/Fdw.Configuration.SourceGenerators.Tests.csproj -c Release`
- `dotnet test public/tests/configuration/Fdw.Configuration.Tests/Fdw.Configuration.Tests.csproj -c Release`
- `dotnet test public/tests/configuration/Fdw.Services.Settings.Tests/Fdw.Services.Settings.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (10), `data` (8), `collections` (7), `results` (4), `abstractions` (3), `commands` (3), `analyzers` (2), `authentication` (2), `endpoints` (2), `services` (2), `authorization` (1), `pipelines` (1), `resiliency` (1), `security` (1), `validation` (1).
Depended on by: `connections` (19), `pipelines` (19), `authentication` (15), `credentials` (10), `data` (9), `datasets` (6), `notifications` (6), `telemetry` (6), `ui` (5), `multitenancy` (4), `scheduling` (4), `users` (4), `aegis` (3), `authorization` (3), `calculations` (3), `endpoints` (3), `hosts` (3), `quality` (3), `secretmanagers` (3), `services` (3), `resiliency` (2), `shells` (2), `catalog` (1), `schema` (1), `transformations` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `configuration`, `catalog-settings`.
