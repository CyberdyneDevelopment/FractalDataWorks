# Fdw.Services.Connections.PostgreSql

PostgreSQL connection components.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PostgreSqlAuthenticationTypes` | class | TypeCollection of PostgreSql authentication configurations. Each entry is both a TypeOption (identity +… |
| `PostgreSqlResultCodes` | class | TypeCollection for PostgreSQL connection result codes. |

## Options (7 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AuthenticationValidationFailedCode` | class | Authentication validation failed for PostgreSQL connection. |
| `NonePostgreSqlAuthentication` | class | No authentication — relies on the server's own trust/peer configuration. No KVP keys required. |
| `PasswordPostgreSqlAuthentication` | class | Username/password authentication. KVP keys: Username, SecretKeyName, SecretManagerName. |
| `SourceMissingContainerNameCode` | class | Source configuration is missing ContainerName — cannot resolve to a PostgreSQL table container. |
| `PostgreSqlConnectionConfigurationCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.Connections.PostgreSql --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Data.DataSets` · `Fdw.Data.Files` · `Fdw.Data.PostgreSql` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
