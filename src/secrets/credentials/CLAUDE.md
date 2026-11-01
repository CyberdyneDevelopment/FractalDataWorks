# The credentials domain

Credentials: credential stores, the data vault and token managers.

This folder, `public/src/secrets/credentials/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/secrets/credentials/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/secrets/credentials/`.

## Projects

fractaldataworks:

- `Fdw.Services.Credentials`: Credential service domain for Fdw — collection, option base, provider and configuration.
- `Fdw.Services.Credentials.Abstractions`
- `Fdw.Services.Credentials.Sql`: SQL Server-backed credential service (vault-backed PAT and agent key) for Fdw.
- `Fdw.Services.DataVault`: Data vault domain for Fdw — collection, option base, provider and configuration.
- `Fdw.Services.DataVault.Abstractions`
- `Fdw.Services.TokenManagers`
- `Fdw.Services.TokenManagers.Abstractions`

reference-servicetypes:

- `FdwReferenceCredentials.Sql`: Sql credential service — the aggregation over FDW's Credentials machinery, plus its service type registration.
- `FdwReferenceCredentials.Sql.Registration`: The SQL credential service-type registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ICredentialService` (Fdw.Services.Credentials.Abstractions)
- `ICredentialOutcome` (Fdw.Services.Credentials.Abstractions)
- `ICredentialServiceProvider` (Fdw.Services.Credentials.Abstractions)
- `IDataVault` (Fdw.Services.DataVault.Abstractions)
- `ITokenIssuer` (Fdw.Services.TokenManagers.Abstractions)
- `ITokenRevocationStore` (Fdw.Services.TokenManagers.Abstractions)
- `IDataVaultImplementationConfiguration` (Fdw.Services.DataVault.Abstractions)
- `ICredentialServiceImplementationConfiguration` (Fdw.Services.Credentials.Abstractions)
- `ISigningCredentialProvider` (Fdw.Services.TokenManagers.Abstractions)
- `CredentialOutcomeBase` (Fdw.Services.Credentials.Abstractions)
- `ICredentialServiceFactory` (Fdw.Services.Credentials.Abstractions)
- `IDataVaultFactory` (Fdw.Services.DataVault.Abstractions)
- `ITokenManagerImplementationConfiguration` (Fdw.Services.TokenManagers.Abstractions)
- `IDataVaultProvider` (Fdw.Services.DataVault.Abstractions)
- `ICredentialServiceConfigurationProvider` (Fdw.Services.Credentials.Abstractions)
- `ICredentialServiceType` (Fdw.Services.Credentials.Abstractions)
- `ICredentialVault` (Fdw.Services.Credentials.Abstractions)
- `IDataVaultConfigurationProvider` (Fdw.Services.DataVault.Abstractions)
- `IDataVaultType` (Fdw.Services.DataVault.Abstractions)
- `ITokenManagerConfigurationProvider` (Fdw.Services.TokenManagers.Abstractions)
- and 6 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/secrets/credentials/FdwReferenceCredentials.Sql.Tests/FdwReferenceCredentials.Sql.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `messagelogging` (15), `data` (14), `collections` (13), `services` (11), `configuration` (10), `results` (7), `secretmanagers` (7), `commands` (6), `abstractions` (4), `connections` (4), `authentication` (3), `users` (2), `authorization` (1), `multitenancy` (1), `pipelines` (1), `security` (1), `validation` (1).
Depended on by: `users` (3), `authentication` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-cred`.
