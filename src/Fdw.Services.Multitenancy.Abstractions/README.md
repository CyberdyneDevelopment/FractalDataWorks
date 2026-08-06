# Fdw.Services.Multitenancy.Abstractions

The tenancy contracts — tenant context, tenant resolution and the org model above it.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (11)

| Type | Kind | Purpose |
|---|---|---|
| `IMutableOrgContext` | interface | Mutable org context for setting the current organization during request processing. Set by… |
| `IMutableTenantContext` | interface | Mutable tenant context for setting the current tenant. |
| `IOrgContext` | interface | Provides access to the current organization context. Scoped per-request. Resolved after tenant context… |
| `IOrganizationProvider` | interface | Provides access to organization records backed by tenant.Organizations. |
| `IRequestTenantInfo` | interface | Provides access to the current tenant context from HTTP request. |
| `ITenant` | interface | Represents a tenant in a multi-tenant system. Tenants are registered via configuration and can have… |
| `ITenantContext` | interface | Provides access to the current tenant context. Scoped per-request in web scenarios. |
| `ITenantOptions` | interface | Represents custom options/settings for a tenant. |
| `ITenantProvider` | interface | Provides tenant resolution and management. |
| `ITenantResolutionContext` | interface | Context for tenant resolution from requests. |
| `ITenantTheme` | interface | Represents theme configuration for a tenant. |

## Base types (1)

| Type | Kind | Purpose |
|---|---|---|
| `TenantTypeBase` | class | Base class for tenant type options. Tenants can be defined statically or loaded from configuration at… |

## Models and supporting types (22)

| Type | Kind | Purpose |
|---|---|---|
| `ConfiguredTenant` | class | Dynamic tenant created from configuration at runtime. |
| `CreateTenantRequest` | class | Data transfer object for creating a new tenant. |
| `MutableOrgContext` | class | Request-scoped org context implementation. Registered as a scoped service; OrgResolutionMiddleware calls… |
| `MutableTenantContext` | class | Request-scoped tenant context implementation. |
| `NullOrganizationProvider` | class | Null-object implementation of . Registered when multitenancy is disabled. All queries return failure so… |
| `NullTenantProvider` | class | Null-object implementation of , registered by the SingleTenant multitenancy option. All operations… |
| `OrganizationConfiguration` | class | Configuration for an organization within a tenant. Maps to tenant.Organizations. Each tenant has one or… |
| `SetDefaultTenantResponse` | class | Response confirming the new default tenant. |
| `SwitchTenantRequest` | class | Request to switch the current user's active tenant. |
| `SwitchTenantResponse` | class | Response for tenant switch operation. |
| `TenantConfiguration` | class | Configuration for a tenant, bindable from appsettings.json. Add new tenants by adding sections under… |
| `TenantDetailPayload` | class | Represents detailed information about a tenant. |

## Installation

```bash
dotnet add package Fdw.Services.Multitenancy.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
