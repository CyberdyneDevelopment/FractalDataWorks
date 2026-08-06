# Fdw.Services.Connections.Sqlite

SQLite connection components.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SqliteAuthenticationTypes` | class | TypeCollection of SQLite authentication methods. Each option parses the connection's authentication KVP… |

## Options (5 declared)

| Type | Kind | Purpose |
|---|---|---|
| `EncryptionKeySqliteAuthentication` | class | Encryption-key SQLite authentication — an encrypted database file (SQLCipher/SEE) whose key is resolved… |
| `NoneSqliteAuthentication` | class | No SQLite authentication — a plain, unencrypted database file. No secret is resolved. KVP keys: (none). |
| `SqliteConnectionConfigurationCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.Connections.Sqlite --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataSets` · `Fdw.Data.Sqlite` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
