# Authentication Architecture

This guide documents the FractalDataWorks authentication stack: server-side token issuance and validation through the **TokenManager** domain (OpenIddict, RS256 JWT), client-side token management, and the DelegatingHandler pipeline that connects them.

## Server-Side: The TokenManager Domain

The token service is the `TokenManagerTypes` provider axis. `ITokenManager` (`Issue` / `Validate` / `Invalidate` / `ExtractClaims`) is the single seam; `OpenIdTokenManager` (the `"OpenIddict"` option) is the reference implementation — it mints RS256 access tokens and manages refresh-token rotation. FDW owns credential validation and claim baking, keeping the token service IdP-agnostic. All server-side authentication flows through `IUserService` + `IUserCredentialService` (vault-backed). See [Building a Token Manager](10-03-Building-Authentication-Service.md) for the domain build-out.

> **Replaces the auth-server split.** The capability-routed `AuthServer` / `AuthenticationServer` / `AuthorizationServer` / `AuthService` collections and `ITokenIssuanceService` are **deleted**. There is one provider axis (`TokenManagerTypes`) and one interface (`ITokenManager`).

### Package Map (Server)

```
Services.TokenManagers.Abstractions   ITokenManager, IAuthenticationService, TokenIssuanceRequest
Services.TokenManagers                TokenManagerTypes ([ServiceTypeCollection]), AuthenticationService
                                      (generic, provider-agnostic authN), TokenManagerConfigurationProvider
Services.Authentication.OpenIddict    OpenIddictTokenManagerType ([ServiceTypeOption "OpenIddict"]),
                                      OpenIdTokenManager, ConnectTokenEndpoint, ProcessSignInClaimsHandler,
                                      ExternalIdentityService, RevokedAccessTokenStore
Services.Users(.Abstractions)         IUserService, IUserCredentialService (vault-backed)
Services.Credentials(.Sql)            ICredentialService, CredentialServiceTypes (named indirection over the vault)
Services.DataVault / Credentials.Sql  IDataVault (verify-only credential store)
Services.SecretManagers.*             ISecretManager provider (RS256 signing key, OAUTH_* client secrets)
```

### Login Flow: password grant

```
POST /connect/token  {grant_type=password, username, password, [tenant, org]}
        |
        v
ConnectTokenEndpoint.HandleAsync()          (Services.Authentication.OpenIddict)
  builds a TokenIssuanceRequest
        |
        v
IAuthenticationService.Authenticate(request)   (generic AuthenticationService)
        |
        +-- password/agent_key ONLY: IUserCredentialService.Verify(userId, "Password", credential)
        |     resolves the credential service by name via ICredentialServiceProvider;
        |     forwards to the configured vault; PBKDF2 compare inside the vault command;
        |     returns an ICredentialOutcome (GrantsAccess true only on Match)
        |
        +-- ResolveActiveTokenManager(): reads auth.TokenManager headers, requires exactly ONE
        |     enabled row, resolves the ITokenManager by name via IFdwServiceProvider
        |
        v
OpenIdTokenManager.Issue(request)           (the active ITokenManager)
        |
        v  IssueForCredential(request, isAgentKey: false, ct)
        |
        +-- UserConfigurationProvider.GetUser(username)    ← DataGateway → usr.Users (user must be active)
        +-- IUserCredentialService.Verify(...) (re-checked at the provider seam)
        +-- BuildIdentityPrincipal(user.Id, roles:[], tenantId, orgId, isCrossTenant)
              thin ClaimsPrincipal (sub + optional tenant/cross-tenant claims)
        |
        v
ConnectTokenEndpoint: principal.SetScopes(scopes)
        |
        v
Results.SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
        |
        v  OpenIddict's ProcessSignIn pipeline fires
        |
        v
ProcessSignInClaimsHandler.HandleAsync()
        bakes the full FDW claim set: tenant_id, org_id, role, perm
        single claim-baking site — never baked in the endpoint or token manager for interactive grants
        |
        v
OpenIddict mints RS256 access token + refresh token
RS256 signing key resolved on demand via the ISecretManager provider (OpenIddictSigningKeyConfigurator)
```

**DG** = IDataGateway. User queries go through DataGateway, not raw SQL.

### agent_key Grant

Same as `password` but `isAgentKey: true` → secretType `"AgentKey"` → an additional `agent` role added to the thin principal. Agent-key verification goes through `IUserCredentialService.Verify(userId, "AgentKey", credential)` against the same credential vault.

### external_identity Grant

`OpenIdTokenManager.IssueForExternalIdentity` maps an external principal's `iss`/`sub` claims to a FDW `userId` via `ExternalIdentityService.FindUserId(provider, externalSubject)` (reads `auth.ExternalIdentity` rows). Returns the same thin `ClaimsPrincipal`; `ProcessSignInClaimsHandler` bakes the FDW claim set identically.

### client_credentials Grant

Machine-to-machine tokens carry the `client_id` as subject with no interactive user. `OpenIdTokenManager.IssueForClientCredentials` resolves the shared secret from the header's configured secret manager under the `OAUTH_{CLIENTID}` convention and constant-time-compares it (`CryptographicOperations.FixedTimeEquals`). OpenIddict's own `ValidateClientSecret` event handler is **removed** (`RemoveEventHandler(...ValidateClientSecret.Descriptor)`) so the token service owns the check — the seeded confidential-client row's `ClientSecretHash` stays `NULL`, which keeps its permissions linked. The service principal's effective permissions are baked as `perm` claims here (this grant is in `ProcessSignInClaimsHandler`'s skip-set). Missing config, missing secret, or a mismatch all fail loud.

### refresh_token / authorization_code Grant

`ConnectTokenEndpoint` authenticates the stored OpenIddict principal and re-signs in — `ProcessSignInClaimsHandler` re-resolves permissions on every refresh, so permission changes take effect on the next token cycle. The caller may supply a `tenant=` param to switch tenants on refresh.

### Claims Baking — ProcessSignInClaimsHandler

All FDW claims (`tenant_id`, `org_id`, `role`, `perm`) for interactive grants are baked here, after the endpoint calls `SignIn`. The endpoint hands a thin principal (only `sub` + optional tenant/cross-tenant context); `ProcessSignInClaimsHandler` resolves the full FDW authorization context. `client_credentials` is skipped (its `perm` claims are already baked by `OpenIdTokenManager.Issue`).

### Signing Key

The RS256 signing key and issuer are resolved on demand by `OpenIddictSigningKeyConfigurator` (an `IConfigureOptions<OpenIddictServerOptions>`) through the gateway-backed config providers and the `ISecretManager` provider — the same injected-provider path every other FDW service uses, no hosted service, no mutable singleton. The key name lives on the header (`TokenManagerConfiguration.SecretManagerName` / `SecretKeyName`). Never stored in appsettings. `OpenIdTokenManager.Validate`/`ExtractClaims` verify against the same key.

### Provider-Swap Story

The token service is a `[ServiceTypeOption]` on `TokenManagerTypes`. Swapping providers (e.g. adding Entra/external IdP support) requires:

1. Create `Services.Authentication.MyProvider` with a new `[ServiceTypeOption(typeof(TokenManagerTypes), "MyProvider")]` implementing `ITokenManager`.
2. Or reuse the `external_identity` grant + `auth.ExternalIdentity` mapping rows to route external subjects to FDW user IDs — no new option needed.
3. The `password` vault path is simply not invoked for external-IdP users.
4. PATs and agent keys remain vault-backed (`Services.Credentials.Sql`, the `Sql` option of `CredentialServiceTypes`) regardless of IdP — the vault is IdP-agnostic.

### Registration (OpenIddict)

`TokenManagerTypes` is a `[ServiceTypeCollection]` and is **discovered by PlatformServices** like every other domain — there is **no** hand-written `TokenManagerTypes.Configure/Register/Initialize` call in `Program.cs`, and it is not `Manual`. `OpenIddictTokenManagerType` is the `[ServiceTypeOption]` that registers OpenIddict's infrastructure (`AddCore` + `AddServer` + `AddValidation`, the DataGateway-backed stores, `ConnectTokenEndpoint`, `ProcessSignInClaimsHandler`) inside its `RegisterRequiredServices` — the one registration surface. Its `Registration.SourceGenerators` module initializer registers the option on package reference. No app-side `services.AddXxx` calls for token-service internals. Point a deployment at a provider by seeding one enabled `auth.TokenManager` row (`ServiceOptionType`) plus its typed-body row.

## Client-Side Architecture

### Shared Package: `Web.Http.Authentication`

Core abstractions and DelegatingHandlers used by all hosting models:

**`IAccessTokenProvider`** -- Returns the current access token. Implementations differ by hosting model:
- `BlazorServerAccessTokenProvider` -- reads from circuit AsyncLocal, falls back to HttpContext
- `JwtAuthService` (WASM) -- reads from localStorage, auto-refreshes if expiring

**`ITokenRefreshHandler`** -- Coordinates token refresh:
- `CanRefresh` property indicates whether refresh is supported
- `TryRefresh(ct)` attempts to obtain a new token pair
- `JwtAuthService` implements this for WASM

**`ITokenRefreshCoordinator`** -- Prevents concurrent refresh calls. When a dashboard loads 5-8 API calls simultaneously and the token is expiring, all calls see `IsTokenExpiring=true` and attempt refresh. Without coordination, multiple concurrent refresh requests hit the server -- if the server rotates refresh tokens (OWASP best practice), all but the first fail and the user gets logged out.

The default implementation (`DefaultTokenRefreshCoordinator`) uses `SemaphoreSlim(1,1)` with a timestamp cooldown:

```csharp
// First caller acquires the gate and refreshes
// Subsequent callers wait, then see the recent refresh and skip
await _gate.WaitAsync(ct);
try
{
    if (DateTimeOffset.UtcNow - _lastRefreshAt < CooldownWindow)
        return true; // Someone else just refreshed

    var result = await refreshFunc(ct);
    if (result) _lastRefreshAt = DateTimeOffset.UtcNow;
    return result;
}
finally { _gate.Release(); }
```

Custom implementations can provide distributed coordination, custom cooldown logic, or telemetry hooks by implementing `ITokenRefreshCoordinator`.

**`IAuthExpirationNotifier`** -- Called when refresh fails (session truly dead). Clears tokens and triggers auth state change so the UI redirects to login.

**`BearerTokenHandler`** -- DelegatingHandler that attaches the bearer token to every outgoing HTTP request via `IAccessTokenProvider.GetAccessToken()`.

**`RetryingBearerTokenHandler`** -- Same as above, plus: on 401 response, calls `ITokenRefreshHandler.TryRefresh()`, obtains a fresh token, clones the request, and retries once. If refresh fails, notifies `IAuthExpirationNotifier`.

### Blazor Server: `Web.Http.Authentication.Blazor`

Blazor Server has a unique challenge: `HttpContext` is only available during the initial WebSocket handshake, not during subsequent circuit events. The circuit bridge solves this:

```
Browser                    Server
  |                          |
  |--- WebSocket Handshake -->|  HttpContext available
  |                          |  TokenCapturingCircuitHandler.OnConnectionUpAsync()
  |                          |    captures access_token from HttpContext
  |                          |    stores in _capturedToken field
  |                          |
  |--- Circuit Event ------->|  HttpContext is NULL
  |                          |  CreateInboundActivityHandler() fires
  |                          |    sets CircuitTokenAccessor.CurrentToken = _capturedToken
  |                          |    (AsyncLocal -- flows through async call chain)
  |                          |
  |                          |  BlazorServerAccessTokenProvider.GetAccessToken()
  |                          |    reads CircuitTokenAccessor.CurrentToken
  |                          |    returns token for BearerTokenHandler
```

**Registration:**
```csharp
services.AddBlazorServerAuthentication();
// Registers: CircuitTokenAccessor (singleton), TokenCapturingCircuitHandler (scoped),
//            HttpContextAccessor, BlazorServerAccessTokenProvider as IAccessTokenProvider
```

### Blazor WASM: `Web.Http.Authentication.Wasm`

All authentication runs in the browser. There is no server-side state.

**`JwtAuthService`** -- Implements `IAuthenticationClient`, `IAccessTokenProvider`, `ITokenRefreshHandler`, and `IAuthExpirationNotifier` in a single class:
- **Login**: `POST auth/token` with credentials, store tokens in localStorage, parse JWT claims, raise `AuthStateChanged`
- **Logout**: Best-effort `POST auth/logout`, clear localStorage, raise `AuthStateChanged`
- **RefreshToken**: `POST auth/refresh` with stored refresh token, replace tokens, re-parse claims
- **GetAccessToken**: Read from localStorage, auto-refresh via `ITokenRefreshCoordinator` if `IsTokenExpiring`
- **TryRestoreAuthState**: On app startup, check localStorage for existing tokens, refresh if expiring

**`JwtAuthStateProvider`** -- Blazor `AuthenticationStateProvider` that:
- On first `GetAuthenticationStateAsync()`, calls `TryRestoreAuthState()` to resume sessions
- Subscribes to `AuthStateChanged` events from `JwtAuthService`
- Maps `UserInfo` claims to `ClaimsPrincipal` for Blazor's `<AuthorizeView>` components

**`LocalStorageTokenService`** -- Stores `fdw_access_token`, `fdw_refresh_token`, and `fdw_token_expiration` in browser localStorage via `IJSRuntime` interop.

**`AuthenticationClients`** -- TypeCollection for pluggable auth. `JwtAuthenticationClientType` is the default; additional types can be added without modifying existing code:

```csharp
[TypeCollection(typeof(AuthenticationClientBase), typeof(IAuthenticationClientType), typeof(AuthenticationClients))]
public abstract partial class AuthenticationClients
    : TypeCollectionBase<AuthenticationClientBase, IAuthenticationClientType> { }

[TypeOption(typeof(AuthenticationClients), "Jwt")]
public sealed class JwtAuthenticationClientType : AuthenticationClientBase { ... }
```

**Registration:**
```csharp
services.AddWasmAuthentication();
// Registers: JwtAuthService as IAuthenticationClient + IAccessTokenProvider +
//            ITokenRefreshHandler + IAuthExpirationNotifier,
//            LocalStorageTokenService, JwtAuthStateProvider,
//            TokenRefreshCoordinator, JwtForgotPasswordProvider
```

## Token Flow: End-to-End

### Login

```
Browser (WASM)                    reference-api                    Database
     |                                |                              |
     |  POST /connect/token           |                              |
     |  {username, password}          |                              |
     |------------------------------->|                              |
     |                                |  IAuthenticationService      |
     |                                |    .Authenticate(request)    |
     |                                |                              |
     |                                |  DataGateway.Execute(query)  |
     |                                |----------------------------->|
     |                                |  User row                    |
     |                                |<-----------------------------|
     |                                |                              |
     |                                |  IUserCredentialService.Verify() (vault)
     |                                |  OpenIddict mints RS256 JWT  |
     |                                |                              |
     |  {accessToken, refreshToken,   |                              |
     |   expiresIn}                   |                              |
     |<-------------------------------|                              |
     |                                                               |
     |  localStorage.setItem(tokens)                                 |
     |  AuthStateChanged -> ClaimsPrincipal                          |
```

### Authenticated Request with Refresh

```
Browser (WASM)                    reference-api
     |                                |
     |  IAccessTokenProvider          |
     |    .GetAccessToken()           |
     |  IsTokenExpiring? Yes          |
     |                                |
     |  ITokenRefreshCoordinator      |
     |    .RefreshOnce(refresh)       |
     |  (serialized -- one at a time) |
     |                                |
     |  POST /connect/token           |
     |  {grant_type=refresh_token}    |
     |------------------------------->|
     |                                |  Validate refresh token
     |                                |  Rotate: issue new pair
     |  {newAccess, newRefresh}       |
     |<-------------------------------|
     |                                |
     |  localStorage.setItem(new)     |
     |                                |
     |  GET /api/v1/connections       |
     |  Authorization: Bearer <new>   |
     |------------------------------->|
     |                                |  OpenIddict validation middleware validates
     |  200 OK                        |
     |<-------------------------------|
```

### 401 Retry (RetryingBearerTokenHandler)

```
Component calls API
     |
     v
RetryingBearerTokenHandler.SendAsync()
     |
     +-- Attach token via IAccessTokenProvider
     +-- Send request
     |
     v
Response = 401 Unauthorized?
     |
     +-- Yes: ITokenRefreshHandler.TryRefresh()
     |        |
     |        +-- Success: Clone request, attach new token, retry
     |        |            Return retry response
     |        |
     |        +-- Failure: IAuthExpirationNotifier.NotifySessionExpired()
     |                     Clear tokens, trigger auth state change
     |                     Return original 401
     |
     +-- No: Return response as-is
```

## Security Considerations

### Token Storage

| Hosting Model | Storage | Exposure |
|---------------|---------|----------|
| Blazor Server | Server memory (AsyncLocal) | Not accessible to browser JS |
| Blazor WASM | localStorage | Accessible to JS -- XSS risk |

For WASM, localStorage is the pragmatic choice. The alternative (HttpOnly cookies via BFF pattern) adds a server-side session layer that defeats the purpose of pure WASM. Mitigate XSS risk with Content Security Policy headers and input sanitization.

### Refresh Token Rotation

OpenIddict rotates refresh tokens on use (OWASP best practice): each refresh issues a new access + refresh pair and the presented refresh token is consumed. Because access tokens are stateless (`DisableTokenStorage`), refresh-token and authorization-code lifecycle is tracked through the persisted **authorization**, not a per-token row. Revoking the authorization (`OpenIdTokenManager.Logout`) invalidates the refresh tokens tied to it.

### Access Token Revocation

Access tokens are stateless RS256 JWTs (`options.DisableTokenStorage()`) -- the server doesn't hit a database on every request. Revocation therefore has a single deny-list path:

- `ITokenManager.Invalidate(token)` reads the token's `jti` and writes an unexpired row to **`auth.RevokedAccessToken`** (via `RevokedAccessTokenStore`), keyed by JWT ID with the token's own expiry.
- `ITokenManager.Validate(token)` verifies the RS256 signature **and** rejects any token whose `jti` has an unexpired `auth.RevokedAccessToken` row.
- For session teardown (logout, password change, role revocation), `OpenIdTokenManager.Logout(subjectId)` revokes the subject's persisted authorizations (and their refresh tokens); already-issued access tokens remain valid until expiry unless separately invalidated.

The revocation list auto-expires entries with the token's own lifetime, so it never grows unbounded.

### Concurrent Refresh Coordination

The `ITokenRefreshCoordinator` prevents a race condition where multiple simultaneous API calls all trigger refresh. Without coordination, refresh token rotation causes all but the first refresh to fail, logging the user out during normal dashboard loads. See `DefaultTokenRefreshCoordinator` for the `SemaphoreSlim` + cooldown implementation.

## Extending the Auth Stack

### Adding a New Token-Manager Provider

Token-service implementations are `[ServiceTypeOption]`s on `TokenManagerTypes`, each implementing `ITokenManager`. There is no `switch` on auth type — the active provider is resolved by name. To add one:

1. Create a `Services.Authentication.MyProvider` project referencing `Fdw.Services.TokenManagers.Abstractions`.
2. Implement `MyTokenManager : ITokenManager` (`Issue` / `Validate` / `Invalidate` / `ExtractClaims`); do all provider-specific credential/secret validation inside `Issue`.
3. Add `MyTokenManagerType : TokenManagerTypeBase<...>` decorated with `[ServiceTypeOption(typeof(TokenManagerTypes), "MyProvider")]`; override `RegisterRequiredServices()` and `RegisterFactory()`.
4. Add a standalone typed-body config (`[ManagedConfiguration(ServiceCategory = "TokenManager", ServiceType = "MyProvider")]`) with a `TokenManagerId` FK for any runtime fields your manager reads.
5. For external-IdP user mapping, add `auth.ExternalIdentity` rows linking provider + external subject to a FDW `userId`, then use the `external_identity` grant.
6. `Registration.SourceGenerators` discovers the option automatically — no app-side registration.

The vault path (`IUserCredentialService.Verify`) is invoked only for `password` and `agent_key` grants. External-IdP options skip the vault entirely for login; PATs and agent keys remain vault-backed regardless.

### Custom Token Refresh Coordination

Implement `ITokenRefreshCoordinator` for scenarios the default doesn't cover:

```csharp
// Example: distributed coordination for server-side Blazor with multiple instances
public sealed class RedisTokenRefreshCoordinator : ITokenRefreshCoordinator
{
    public async Task<bool> RefreshOnce(
        Func<CancellationToken, Task<bool>> refreshFunc,
        CancellationToken ct = default)
    {
        // Acquire distributed lock via Redis
        // Execute refresh
        // Release lock
    }
}
```

Register before calling `AddWasmAuthentication()` (which uses `TryAdd`):
```csharp
services.AddScoped<ITokenRefreshCoordinator, RedisTokenRefreshCoordinator>();
services.AddWasmAuthentication(); // Won't overwrite your registration
```

### Custom Token Storage

Implement `ITokenStorageService` for alternative browser storage:

```csharp
// Example: sessionStorage instead of localStorage
public sealed class SessionStorageTokenService : ITokenStorageService { ... }

// Example: IndexedDB for larger token payloads
public sealed class IndexedDbTokenService : ITokenStorageService { ... }
```

## Related Documentation

- [Building a Token Manager](10-03-Building-Authentication-Service.md) -- the TokenManager domain and adding a provider option
- [Security Hardening](12-04-Security-Hardening.md) -- OWASP headers, CORS, DB isolation
- [Authorization](12-05-Authorization.md) -- RBAC permissions, roles, endpoint policies
- [Secret Management](12-10-Secret-Management.md) -- SecretManager resolution for JWT signing keys and OAUTH_* secrets
- [Service Domains Overview](06-01-Service-Domains-Overview.md) -- ServiceTypeCollection plugin architecture
