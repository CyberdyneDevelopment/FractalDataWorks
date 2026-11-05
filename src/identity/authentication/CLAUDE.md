# The authentication domain

Signing in: authentication services and endpoints, external identity providers, identity (client credentials, JWT assertion), session state and HTTP authentication.

This folder, `public/src/identity/authentication/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/identity/authentication/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/identity/authentication/`.

## Projects

fractaldataworks:

- `Fdw.Services.Authentication`
- `Fdw.Services.Authentication.Abstractions`
- `Fdw.Services.Authentication.Endpoints`: HTTP endpoints for the Authentication domain, extracted from Fdw.Web.Api.
- `Fdw.Services.ExternalIdentityProviders`
- `Fdw.Services.ExternalIdentityProviders.Abstractions`
- `Fdw.Services.Identity`: FDW managed identity domain — short-lived, dynamically-issued tokens proving a service's own identity to peers, in place of static shared secrets.
- `Fdw.Services.Identity.Abstractions`: Abstractions for FDW managed identity — obtaining short-lived, dynamically-issued tokens that prove a service's own identity to peers, in place of static shared secrets.
- `Fdw.Services.Identity.ClientCredentials`: Managed-identity mechanism using the OAuth 2.0 client-credentials grant (RFC 6749 4.4) against any conforming token endpoint. FDW's own OpenIddict authorization server is the usual target, so no external identity provider need be involved.
- `Fdw.Services.Identity.Endpoints`: Endpoint base classes and DTOs for managing and verifying managed identities via FastEndpoints. Never exposes credentials or issued tokens.
- `Fdw.Services.Identity.JwtAssertion`: The JWT-assertion managed identity mechanism (RFC 7523): a workload proves its identity with a signed client assertion minted by an external OIDC issuer, read from a file or an environment variable.
- `Fdw.Services.SessionState`
- `Fdw.Services.SessionState.Abstractions`
- `Fdw.Services.SessionState.Clients`: HTTP client implementation of ISessionStateService for Blazor UI applications.
- `Fdw.Services.SessionState.Clients.Registration`: The session-state HTTP client's [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `Fdw.Web.Http.Authentication`: Bearer token HTTP message handlers for Fdw authentication

reference-servicetypes:

- `FdwReferenceAuth.Endpoints`: The Auth domain HTTP endpoints - authentication, roles, permissions, users, identity and tenants.
- `FdwReferenceAuthentication.AssuranceLevels`: The reference deployment's assurance levels: the values its tokens carry in the acr claim and its flows name in MinimumAcr.
- `FdwReferenceAuthentication.AuthentikSso`: Sign-in type: Authentik SSO. Brings in the steps an AuthentikLogin flow needs -- OidcRedirect, ResolvePrincipal, BakePermissions, AuthorizeSignInMethod, AuthorizeIssuance -- so a host references one package instead of five. No code of its own: referencing this package is the registration.
- `FdwReferenceAuthentication.AuthorizeIssuance.Registration`
- `FdwReferenceAuthentication.AuthorizeSignInMethod.Registration`
- `FdwReferenceAuthentication.BakePermissions.Registration`
- `FdwReferenceAuthentication.ForeignToken`
- `FdwReferenceAuthentication.ForeignToken.Registration`
- `FdwReferenceAuthentication.OidcRedirect`
- `FdwReferenceAuthentication.OidcRedirect.Registration`
- `FdwReferenceAuthentication.Password`: Sign-in type: password login. Brings in the steps a PasswordLogin flow needs -- PasswordCredential, BakePermissions, AuthorizeSignInMethod, AuthorizeIssuance -- so a host references one package instead of four. No code of its own: referencing this package is the registration.
- `FdwReferenceAuthentication.PasswordCredential.Registration`
- `FdwReferenceAuthentication.ResolvePrincipal.Registration`
- `FdwReferenceAuthentication.TenantSwitch.Registration`
- `FdwReferenceAuthentication.TokenProviders`: Access-token providers for outbound HTTP clients — client-credentials acquisition. Forwarding the caller is the framework Session authentication type's own provider (FDW-925).
- `FdwReferenceSessionState.Endpoints`: The SessionState domain's HTTP endpoints, the collections that declare them, and the API service type that drives their registration.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IAuthenticationContextAccessor` (Fdw.Services.Authentication.Abstractions)
- `IAuthenticationContext` (Fdw.Services.Authentication.Abstractions)
- `ISystemRoleConfiguration` (Fdw.Services.Authentication.Abstractions)
- `IExternalIdentityProvisioner` (Fdw.Services.ExternalIdentityProviders.Abstractions)
- `IIdentityService` (Fdw.Services.Identity.Abstractions)
- `IExternalIdentityProvisionerImplementationConfiguration` (Fdw.Services.ExternalIdentityProviders.Abstractions)
- `IFrameworkAuthorizationService` (Fdw.Services.Authentication.Abstractions)
- `IIdentityServiceImplementationConfiguration` (Fdw.Services.Identity.Abstractions)
- `IAuthenticationStep` (Fdw.Services.Authentication.Abstractions)
- `ISessionAuthenticationConfiguration` (Fdw.Services.Authentication.Abstractions)
- `ISessionStore` (Fdw.Services.Authentication.Abstractions)
- `IAuthenticationServiceImplementationConfiguration` (Fdw.Services.Authentication.Abstractions)
- `ISignInLog` (Fdw.Services.Authentication.Abstractions)
- `IAuthenticationServiceConfigurationProvider` (Fdw.Services.Authentication.Abstractions)
- `IAuthenticationFlowProvider` (Fdw.Services.Authentication.Abstractions)
- `ISessionStateService` (Fdw.Services.SessionState.Abstractions)
- `IUserNames` (Fdw.Services.Authentication.Abstractions)
- `AuthenticationMethodBase` (Fdw.Services.Authentication.Abstractions)
- `IPersonalAccessTokenService` (Fdw.Services.Authentication.Abstractions)
- `IIdentityServiceProvider` (Fdw.Services.Identity.Abstractions)
- and 68 more in the `.Abstractions` projects above

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

- `dotnet test public/tests/identity/authentication/Fdw.Services.Authentication.Abstractions.Tests/Fdw.Services.Authentication.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authentication/Fdw.Services.Authentication.Tests/Fdw.Services.Authentication.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authentication/Fdw.Services.ExternalIdentityProviders.Tests/Fdw.Services.ExternalIdentityProviders.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authentication/Fdw.Services.Identity.Tests/Fdw.Services.Identity.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authentication/Fdw.Services.SessionState.Tests/Fdw.Services.SessionState.Tests.csproj -c Release`
- `dotnet test public/tests/identity/authentication/FdwReferenceAuth.Endpoints.Tests/FdwReferenceAuth.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/identity/authentication/FdwReferenceAuthentication.AuthorizeSignInMethod.Registration.Tests/FdwReferenceAuthentication.AuthorizeSignInMethod.Registration.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/identity/authentication/FdwReferenceAuthentication.BakePermissions.Registration.Tests/FdwReferenceAuthentication.BakePermissions.Registration.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/identity/authentication/FdwReferenceAuthentication.OidcRedirect.Tests/FdwReferenceAuthentication.OidcRedirect.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/identity/authentication/FdwReferenceAuthentication.ResolvePrincipal.Registration.Tests/FdwReferenceAuthentication.ResolvePrincipal.Registration.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/identity/authentication/FdwReferenceAuthentication.TokenProviders.Tests/FdwReferenceAuthentication.TokenProviders.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `messagelogging` (28), `data` (22), `services` (18), `collections` (16), `configuration` (15), `commands` (11), `results` (11), `secretmanagers` (8), `pipelines` (6), `authorization` (5), `endpoints` (5), `users` (5), `abstractions` (4), `security` (4), `connections` (3), `multitenancy` (3), `datasets` (2), `registration` (2), `scheduling` (2), `schema` (2), `validation` (2), `calculations` (1), `catalog` (1), `credentials` (1), `hosts` (1), `quality` (1), `ui` (1).
Depended on by: `pipelines` (10), `connections` (6), `endpoints` (6), `authorization` (5), `data` (5), `multitenancy` (4), `scheduling` (4), `catalog` (3), `credentials` (3), `notifications` (3), `quality` (3), `schema` (3), `ui` (3), `users` (3), `workspaces` (3), `agents` (2), `calculations` (2), `configuration` (2), `mcp` (1), `registration` (1), `secretmanagers` (1), `shells` (1), `telemetry` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `authentication`, `catalog-auth`.
