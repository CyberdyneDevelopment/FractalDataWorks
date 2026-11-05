# The authorization domain

Who may do what: roles, permissions, the authorization service, its endpoints and clients.

This folder, `public/src/identity/authorization/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/identity/authorization/`.

## Projects

fractaldataworks:

- `Fdw.Services.Authorization`
- `Fdw.Services.Authorization.Abstractions`
- `Fdw.Services.Authorization.Clients`: API client and registration types for role and permission management operations.
- `Fdw.Services.Authorization.Endpoints`: HTTP endpoints for the Authorization domain, extracted from Fdw.Web.Api.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IScopeAuthority` (Fdw.Services.Authorization.Abstractions)
- `IOrganizationScope` (Fdw.Services.Authorization.Abstractions)
- `IRoleGrants` (Fdw.Services.Authorization.Abstractions)
- `IUserPermissions` (Fdw.Services.Authorization.Abstractions)
- `IGrantRule` (Fdw.Services.Authorization.Abstractions)
- `IOrganizationRoles` (Fdw.Services.Authorization.Abstractions)
- `IPathAuthorizationDecision` (Fdw.Services.Authorization.Abstractions)
- `PermissionActionBase` (Fdw.Services.Authorization.Abstractions)
- `IOrganizationMembers` (Fdw.Services.Authorization.Abstractions)
- `IUserPermissionGrants` (Fdw.Services.Authorization.Abstractions)
- `IPathAuthorizationPolicy` (Fdw.Services.Authorization.Abstractions)
- `PathAuthorizationPolicyBase` (Fdw.Services.Authorization.Abstractions)
- `IUserRoleAssignment` (Fdw.Services.Authorization.Abstractions)
- `IOrganizationBootstrap` (Fdw.Services.Authorization.Abstractions)
- `IPermissionHolders` (Fdw.Services.Authorization.Abstractions)
- `IPlatformAdministratorGuard` (Fdw.Services.Authorization.Abstractions)
- `IRoleMappingImplementationConfiguration` (Fdw.Services.Authorization.Abstractions)
- `IPermissionAction` (Fdw.Services.Authorization.Abstractions)
- `IRoleMappingConfigurationProvider` (Fdw.Services.Authorization.Abstractions)
- `IUserRolesHeld` (Fdw.Services.Authorization.Abstractions)
- and 1 more in the `.Abstractions` projects above

## Rules that matter here

- No fallbacks: a `??` never falls back to a value that could be a real one, and a missing value fails with a MessageLogging error and a failed result. Allowed: a null logger, `?? string.Empty`, `?? []`, `??=` on a field or local, a chain of failure results or messages, a `[PlaceholderText]` constant, a `[PlatformDefault]` member, and a member Mike approved with `[ApprovedFallback]`. Never a default message. FDW055 enforces it.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/identity/authorization/Fdw.Services.Authorization.Abstractions.Tests/Fdw.Services.Authorization.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authorization/Fdw.Services.Authorization.Clients.Tests/Fdw.Services.Authorization.Clients.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authorization/Fdw.Services.Authorization.Tests/Fdw.Services.Authorization.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (8), `data` (7), `pipelines` (6), `authentication` (5), `collections` (5), `services` (5), `endpoints` (4), `commands` (3), `configuration` (3), `connections` (3), `multitenancy` (3), `results` (3), `users` (3), `datasets` (2), `scheduling` (2), `schema` (2), `secretmanagers` (2), `validation` (2), `abstractions` (1), `calculations` (1), `catalog` (1), `hosts` (1), `quality` (1), `registration` (1), `security` (1), `ui` (1).
Depended on by: `authentication` (5), `connections` (3), `endpoints` (3), `multitenancy` (3), `pipelines` (3), `users` (3), `catalog` (2), `data` (2), `quality` (2), `scheduling` (2), `schema` (2), `ui` (2), `calculations` (1), `configuration` (1), `credentials` (1), `notifications` (1), `shells` (1), `workspaces` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `access-model`, `access-roles`, `access-gaps`, `catalog-authz`.
