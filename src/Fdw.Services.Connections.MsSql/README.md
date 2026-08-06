# Fdw.Services.Connections.MsSql

SQL Server connection components: the configuration body, the session-context plan and the pieces a SQL Server connection is composed from.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ForeignKeyActions` | class | TypeCollection for foreign key referential actions. |
| `MsSqlAuthenticationTypes` | class | TypeCollection of MsSql authentication configurations. Each entry is both a TypeOption (identity +… |
| `MsSqlConnectionLimitTypes` | class | TypeCollection of outbound connection limit options for MsSql connections. Mirrors the… |
| `MsSqlConnectionResultCodes` | class | TypeCollection for MsSql Connection result codes. EventId range: 5200-5299 (within… |
| `MsSqlResultCodes` | class | TypeCollection for MsSql connection result codes. EventId range: 5100-5199 (within Connections 5000-5999) |
| `SqlErrorHandlers` | class | TypeCollection of SQL error handlers. Each handler maps one or more SQL Server error numbers to a… |

## Options (51 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AuthenticationTypeUnknownCode` | class | The connection's AuthenticationType does not match any registered MsSql authentication type. |
| `AuthenticationValidationFailedCode` | class | Authentication validation failed. |
| `AuthenticationValueMissingCode` | class | A required authentication property is absent or empty. The authentication type declares it as required,… |
| `AzureCliConfiguration` | class | Azure CLI token-based authentication. No KVP keys required; the access token is acquired via . |
| `BulkCopyFailedCode` | class | Bulk copy operation failed. |
| `ConcurrencyLimitType` | class | TypeOption for the Concurrency connection limit kind. Controls maximum simultaneous in-flight queries… |
| `ConnectionFailedCode` | class | Connection to SQL Server failed. |
| `ConnectionFailedHandler` | class | Handles SQL Server errors -1, 2, and 53: connection failed. The SQL Server instance is unreachable due… |
| `CreationFailedCode` | class | Connection factory failed to create connection. |
| `DailyBudgetLimitType` | class | TypeOption for the DailyBudget connection limit kind. Caps total daily queries and/or bytes via a… |
| `DeadlockCode` | class | SQL deadlock victim (error 1205). The transaction was chosen as a deadlock victim. |
| `DeadlockHandler` | class | Handles SQL Server error 1205: deadlock victim. The transaction was chosen as a deadlock victim and… |
| `DisconnectionFailedCode` | class | Disconnection from SQL Server failed. |
| `EntraIdConfiguration` | class | Microsoft Entra ID (Azure AD) Authentication. KVP keys: AzureAdMode, ClientId, TenantId,… |
| `ExecutionExceptionCode` | class | General execution exception occurred. |
| `InvalidBulkCopyDataTableCode` | class | Missing or invalid DataTable for bulk copy. |
| `LoginFailedCode` | class | SQL login failed (error 18456). The database credentials are incorrect or the login does not exist. |
| `LoginFailedHandler` | class | Handles SQL Server error 18456: login failed. The database credentials are invalid or the login is… |

## Installation

```bash
dotnet add package Fdw.Services.Connections.MsSql --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataNodes` · `Fdw.Data.DataSets` · `Fdw.Data.MsSql` · `Fdw.Data.RowSources.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Types.MsSql` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
