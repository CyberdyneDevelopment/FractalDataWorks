# Fdw.Services.Multitenancy.Sql

SQL Server-backed tenancy: tenant resolution, the request middleware that establishes it, and the session context it sets.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SqlTenantResultCodes` | class | TypeCollection for SQL Tenant result codes. Codes use the categorized-number scheme (Id == EventId ==… |

## Options (12 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ActiveTenantsQueryFailedCode` | class | Active tenants query failed. |
| `AllTenantsQueryFailedCode` | class | All tenants query failed. |
| `InvalidUserIdFormatCode` | class | Invalid user ID format. |
| `TenantAccessValidationFailedCode` | class | Tenant access validation failed. |
| `TenantNotFoundCode` | class | Tenant not found by ID. |
| `TenantQueryFailedCode` | class | Tenant query by ID failed. |
| `TenantResolutionFailedCode` | class | Tenant resolution failed. |
| `TenantSlugNotFoundCode` | class | Tenant not found by slug. |
| `TenantSlugQueryFailedCode` | class | Tenant query by slug failed. |
| `TenantSlugRequiredCode` | class | Tenant slug is required but not provided. |
| `UserIdRequiredCode` | class | User ID is required but not provided. |
| `SqlTenantProviderConfigurationCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.Multitenancy.Sql --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.Hosting.Abstractions` · `Fdw.Results` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Multitenancy` · `Fdw.Services.Multitenancy.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
