# Fdw.Services.Users

The user domain: users, their credentials, roles and tenant access.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `UserServiceTypes` | class | ServiceTypeCollection for user service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultUserServiceType` | class | Default user service type that registers user stores and credential validation with the dependency… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `UserConfiguration` | class | Database-backed configuration for users. Maps to usr.Users in ConfigurationDb. |
| `UserPreferencesConfiguration` | class | Maps to usr.UserPreferences — user display/locale preferences. One row per user (enforced by… |
| `UserTenantConfiguration` | class | Database-backed configuration for user-tenant memberships. Maps to tenant.UserTenants in ConfigurationDb. |

## Installation

```bash
dotnet add package Fdw.Services.Users --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Security.Hashing` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Credentials.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.DataVault.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Services.Users.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
