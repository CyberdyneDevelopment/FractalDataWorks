# The secretmanagers domain

Secret managers: the secret manager service and abstractions, its endpoints, clients and test double.

This folder, `public/src/secrets/secretmanagers/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/secrets/secretmanagers/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/secrets/secretmanagers/`.

## Projects

fractaldataworks:

- `Fdw.Services.SecretManagers`
- `Fdw.Services.SecretManagers.Abstractions`
- `Fdw.Services.SecretManagers.Clients`: API clients and DTOs for secret manager operations.
- `Fdw.Services.SecretManagers.Endpoints`: Generic endpoint base classes and DTOs for managing secret manager configurations via FastEndpoints.

reference-servicetypes:

- `FdwReferenceSecretManagers.AzureKeyVault`: AzureKeyVault secret manager — the aggregation over FDW's SecretManagers machinery, plus its service type registration.
- `FdwReferenceSecretManagers.AzureKeyVault.Registration`: The AzureKeyVault secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSecretManagers.EnvironmentVariable`: EnvironmentVariable secret manager — the aggregation over FDW's SecretManagers machinery, plus its service type registration.
- `FdwReferenceSecretManagers.EnvironmentVariable.Registration`: The EnvironmentVariable secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSecretManagers.HashiCorpVault`: HashiCorp Vault secret manager for FDW: reads stored secrets from the KV engine, and has Vault issue short-lived database credentials from the database engine.
- `FdwReferenceSecretManagers.HashiCorpVault.Registration`: The HashiCorpVault secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSecretManagers.MsSql`: MsSql secret manager — the aggregation over FDW's SecretManagers machinery, plus its service type registration.
- `FdwReferenceSecretManagers.MsSql.Registration`: The MsSql secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSecretManagers.Sqlite`: Sqlite secret manager — the aggregation over FDW's SecretManagers machinery, plus its service type registration.
- `FdwReferenceSecretManagers.Sqlite.Registration`: The Sqlite secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceSecretManagers.UserSecrets`: UserSecrets secret manager — the aggregation over FDW's SecretManagers machinery, plus its service type registration.
- `FdwReferenceSecretManagers.UserSecrets.Registration`: The UserSecrets secret-manager service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ISecretManagerCommand` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManager` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerProvider` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerImplementationConfiguration` (Fdw.Services.SecretManagers.Abstractions)
- `SecretManagerResultCodeBase` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerConfigurationProvider` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerType` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerServiceFactory` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretMetadata` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretType` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerExecutionContext` (Fdw.Services.SecretManagers.Abstractions)
- `SecretTypeBase` (Fdw.Services.SecretManagers.Abstractions)
- `SecretManagerCommandHandlerBase` (Fdw.Services.SecretManagers.Abstractions)
- `SecretManagerMessageCollectionBase` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerCommandHandler` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretContainerUsage` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretContainer` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerConfiguration` (Fdw.Services.SecretManagers.Abstractions)
- `ISecretManagerResultCode` (Fdw.Services.SecretManagers.Abstractions)

## Rules that matter here

- Secrets are written and deleted only through the connection credential store, inside creating, updating or deleting a connection.
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

- `dotnet test public/tests/secrets/secretmanagers/Fdw.Services.SecretManagers.Abstractions.Tests/Fdw.Services.SecretManagers.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/secrets/secretmanagers/Fdw.Services.SecretManagers.TestDouble/Fdw.Services.SecretManagers.TestDouble.csproj -c Release`
- `dotnet test public/tests/secrets/secretmanagers/Fdw.Services.SecretManagers.Tests/Fdw.Services.SecretManagers.Tests.csproj -c Release`
- `dotnet test public/tests/secrets/secretmanagers/FdwReferenceSecretManagers.AzureKeyVault.Tests/FdwReferenceSecretManagers.AzureKeyVault.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/secrets/secretmanagers/FdwReferenceSecretManagers.EnvironmentVariable.Tests/FdwReferenceSecretManagers.EnvironmentVariable.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/secrets/secretmanagers/FdwReferenceSecretManagers.MsSql.Tests/FdwReferenceSecretManagers.MsSql.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/secrets/secretmanagers/FdwReferenceSecretManagers.Sqlite.Tests/FdwReferenceSecretManagers.Sqlite.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/secrets/secretmanagers/FdwReferenceSecretManagers.TestDouble/FdwReferenceSecretManagers.TestDouble.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `collections` (5), `messagelogging` (5), `services` (4), `configuration` (3), `data` (3), `endpoints` (3), `connections` (2), `authentication` (1), `commands` (1), `pipelines` (1), `results` (1), `validation` (1).
Depended on by: `connections` (9), `authentication` (8), `credentials` (7), `data` (5), `users` (4), `aegis` (3), `pipelines` (3), `scheduling` (3), `authorization` (2), `catalog` (2), `endpoints` (2), `multitenancy` (2), `quality` (2), `schema` (2), `security` (2), `telemetry` (2), `ui` (2), `hosts` (1), `mcp` (1), `shells` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `secretmanagers`, `catalog-sec`, `access-resources`.
