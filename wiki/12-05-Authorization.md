# 12-05 Authorization

This guide covers the FDW RBAC (Role-Based Access Control) authorization system. The authorization layer is provider-agnostic -- any authentication provider that populates `ClaimsPrincipal` (JWT, Entra ID, Cognito, etc.) automatically works with the database-backed permission checks.

## Architecture

```
Authentication Provider (JWT, Entra, Cognito, etc.)
        | produces
ClaimsPrincipal (UserId, Roles, Claims)
        | adapted by
ClaimsPrincipalAuthenticationContext → IAuthenticationContext
        | consumed by
FrameworkPermissionHandler (ASP.NET Core AuthorizationHandler)
        | calls
IFrameworkAuthorizationService.Authorize(context, resource, action)
        | reads from
Permission/Role/RolePermission/UserRole configuration providers
        ← authz.Permission / authz.Role / authz.RolePermission / authz.UserRole
          (ConfigurationDb, read via IConfigurationGateway)
```

## Permission Model

Permissions follow a `{resource}:{action}` naming convention:

| Resource | Actions | Examples |
|----------|---------|---------|
| connections | read, write, delete | `connections:read` |
| datastores | read, write, delete | `datastores:write` |
| datasets | read, write, delete | `datasets:delete` |
| pipelines | read, write, execute | `pipelines:execute` |
| schedules | read, write, delete | `schedules:read` |
| users | read, write, delete | `users:write` |
| configurations | read, write | `configurations:read` |

## Role Hierarchy

Three built-in roles with hierarchical permissions:

| Role | Direct Permissions | Effective (via inheritance) |
|------|-------------------|---------------------------|
| **Viewer** | 7 read permissions | 7 total |
| **Operator** | 7 write/execute permissions | 14 total (inherits Viewer reads) |
| **Admin** | 6 delete/config-write permissions | 20 total (inherits Operator + Viewer) |

Hierarchy is implemented via `ParentRole` on `RoleBase`. The `GetEffectivePermissions()` method walks the parent chain to aggregate all permissions.

## Hosting Integration

Add authorization to the fluent builder:

```csharp
builder.AddFrameworkServiceTypes(loggerFactory, types =>
{
    types.AddSecretManagers()
         .AddConnections()
         .AddDataStores(ds => ds.RegisterMsSql())
         .AddDataSets()
         .AddAuthentication()
         .AddAuthorization()   // ← Registers FDW RBAC bridge
         .AddEtlPipelines()
         .AddDataGateway();
});
```

`AddAuthorization()` registers:
- `FdwAuthorizationPolicyProvider` -- dynamically creates policies for `fdw:{resource}:{action}` patterns
- `FrameworkPermissionHandler` -- bridges ASP.NET Core authorization to `IFrameworkAuthorizationService`
- `DefaultAuthorizationService` -- reads permissions and roles from database via `IOptionsMonitor`

## Endpoint Authorization

Endpoints use the `Policies()` method with `fdw:{resource}:{action}` format:

```csharp
public override void Configure()
{
    Get("/api/v1/connections");
#if DEVELOP
    AllowAnonymous();
#else
    Policies("connections:read");
#endif
}
```

The `#if DEVELOP` guard allows unauthenticated access during local development while Release builds enforce authorization.

### Verb-to-Action Mapping

| HTTP Verb | Action | Example Policy |
|-----------|--------|----------------|
| GET / list | read | `connections:read` |
| POST (create) / PUT (update) | write | `connections:write` |
| DELETE | delete | `connections:delete` |
| POST (trigger/execute) | execute | `pipelines:execute` |

### Endpoints That Stay AllowAnonymous

These endpoints never require authorization:

- `TokenEndpoint` -- login (produces JWT)
- `RefreshTokenEndpoint` -- token refresh
- `HealthEndpoint` -- health check
- `PublicDataEndpoint` -- public demo data
- `NflEndpoints` -- public NFL statistics

## Adding Custom Permissions

### 1. Create a Permission TypeOption

```csharp
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Permissions), "myresource:read", RestrictToCurrentCompilation = true)]
public sealed class MyResourceReadPermission : PermissionBase
{
    public MyResourceReadPermission()
        : base(21, "myresource", "read", "mdi-icon", "Info", "Read my resources") { }
}
```

### 2. Add to Role

Add the permission to the appropriate role's `Permissions` property.

### 3. Seed Database

Insert into `authz.Permission` and `authz.RolePermission` tables.

### 4. Apply to Endpoint

```csharp
Policies("myresource:read");
```

## Database Tables

| Table | Schema | Purpose |
|-------|--------|---------|
| `authz.Permission` | authz | Permission definitions (resource + action) |
| `authz.Role` | authz | Role definitions with system flag |
| `authz.RolePermission` | authz | Maps roles to permissions |
| `authz.UserRole` | authz | Maps users to roles |

## Plugin Architecture

The authorization layer is decoupled from authentication. Any provider that populates `ClaimsPrincipal` works:

1. **JWT** (built-in) -- extracts `sub`, `name`, `role` claims
2. **Entra ID / Azure AD** -- populates ClaimsPrincipal via OIDC
3. **AWS Cognito** -- populates ClaimsPrincipal via JWT
4. **Custom** -- any provider implementing ASP.NET Core authentication

The `ClaimsPrincipalAuthenticationContext` adapter extracts:
- `UserId` from `sub` / `nameidentifier` / `name` claims
- `Username` from `name` claim
- `Roles` from `role` claims
- `ExpiresAt` from `exp` claim

## Service-to-Service Communication

`InternalApiKeyMiddleware` handles trust between services (reference-etl, reference-scheduler). This is separate from user authorization -- it validates the `X-Internal-Api-Key` header for service-to-service calls. See [12-04 Security Hardening](12-04-Security-Hardening.md).
