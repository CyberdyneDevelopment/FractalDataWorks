# Authentication Architecture

This page describes how FractalDataWorks issues and checks access tokens. There are three parts:

- **Issuing** a token is a configured *flow* of authentication steps, run by `AuthenticationRunner`. The result is an RS256 JWT minted by `JwtTokenIssuer`.
- **Checking** a token is one of the schemes on `AuthenticationServiceTypes`, chosen per request by the token's issuer.
- **Clients** attach and renew tokens through the DelegatingHandlers in `Web.Http.Authentication`.

There is no OpenIddict, no `/connect/token` endpoint and no `ITokenManager`. The step pipeline replaced all of them (FDW-672). For the mechanism in depth, with a source citation for each claim, read `fdw-anatomy/content/authentication.json`.

## Server Side: Issuing a Token

### A login is a flow of steps

A flow is two kinds of configuration row: `auth.AuthenticationFlow` (name, audience, minimum assurance level, execution lifetime, the secret that encrypts a suspended login, and the allowed return URLs) and `auth.AuthenticationFlowStepConfiguration` (the ordered step names). `AuthenticationFlowProvider` loads and validates every flow on first use. It caches them until the host restarts. That is deliberate: a flow never changes under a login in progress. A flow whose steps are missing or badly ordered is refused by name. It never blocks another flow.

A step is an option on `AuthenticationStepTypes`, and the option *is* the step: it implements `IAuthenticationStep`. Each step declares three things:

| Declaration | Meaning |
|---|---|
| `Requires` | The context elements that must already be present before the step runs. |
| `Contributes` | The elements the step may add. The runner discards and logs anything else it returns. |
| `AuthenticationMethods` | The RFC 8176 methods the step proves, such as `pwd`. |

The context elements are a closed set: `Subject`, `Principal`, `Claims` and `Decision`. Steps are open. A new proof ships as a package, and the runner does not change.

The steps that ship live in reference-servicetypes, one package per step (`ReferenceAuthentication.<Step>.Registration`):

| Step | Requires | Contributes | What it does |
|---|---|---|---|
| `PasswordCredential` | none | Subject, Principal | Verifies a username and password against the platform's own credential store. Proves `pwd`. |
| `TenantSwitch` | none | Subject, Principal | Identifies an already-authenticated caller again, acting in another of their tenants. |
| `OidcRedirect` | none | Subject, Claims | Sends the browser to an external OIDC provider, then exchanges the returned code. |
| `ForeignToken` | none | Subject, Claims | Verifies a token another provider issued, for a client that ran its own login. |
| `ResolvePrincipal` | Subject | Principal | Binds a proved external subject to a local user, or provisions one. |
| `BakePermissions` | Principal | Claims | Resolves the user's roles and permissions in the tenant the token is for, and bakes them as `perm` and `roles` claims. |
| `AuthorizeIssuance` | Principal | Decision | Decides whether a token may be issued. |

### The runner

`AuthenticationRunner` runs a flow's steps in order:

1. It checks each step's `Requires` against the context before running it.
2. It merges only what the step declared it `Contributes` (rule I1).
3. It records the step's authentication methods once the step succeeds. From those it derives the assurance level (`acr`). A step can never name its own method.
4. A step may *suspend* the flow, for example to send the browser to an external provider. The runner stores the context, encrypted with the flow's key, through `IAuthenticationExecutionStore` (`DatabaseExecutionStore`, table `auth.AuthenticationExecution`). A suspended login therefore survives a restart. `AuthenticationRunner.Resume` finds the flow by name from the stored record.
5. The terminal check runs after the last step. It is code, not a step, so no configuration can skip it. A token is minted only if there is a subject, a principal, a decision that permits issuance, and an assurance level that meets the flow's minimum.

The runner then builds an `IssuanceRequest` and hands it to `ITokenIssuer`. Only claims from this platform (`Local` or `Derived` sources) reach the token. A role named by an external provider grants nothing.

### Issuance

`JwtIssuanceResolver` reads the `auth.TokenManager` rows, selects the JWT one (`auth.JwtTokenManager`: `Issuer`, `AccessTokenLifetime` as an ISO 8601 duration, and the secret manager and key name of the signing key), and builds one `JwtTokenIssuer`. A missing issuer, lifetime or key location fails loud. The signing key is a PEM RSA private key read through `SecretManagerSigningCredentialProvider`.

`JwtTokenIssuer` mints an `at+jwt` token signed RS256. It carries:

- `sub` and `ClaimTypes.NameIdentifier`: the principal id.
- `jti`: a version-7 GUID, so a single token can be revoked.
- `tenantId`: the tenant the token acts in.
- `scope`, and `amr` and `acr` from the runner.
- Every `Local` or `Derived` claim the steps gathered. Repeated claims (`perm`, `roles`) stay arrays.
- `exp`: the configured lifetime, capped by the claims it carries. `BakePermissions` reports the earliest `ExpiresAt` among the role assignments it used (`ContextContribution.ClaimsValidUntil`). The token never outlives that. A token whose claims have already expired is refused, not minted (FDW-862).

### HTTP endpoints (reference-servicetypes `ReferenceAuth.Endpoints`)

| Endpoint | Purpose |
|---|---|
| `POST /auth/login` | Username and password. Runs the password flow and returns the token. |
| `POST /auth/oidc/redirect` | Body `{flowName, returnUrl}`. Starts an external-provider flow and returns `{redirectUrl}` as JSON; the browser navigates there itself. `returnUrl` must be one of the flow's configured `ReturnUrls`; any other value answers 400. |
| `GET /auth/oidc/callback` | The provider returns here. The flow resumes, and the browser is redirected to the return URL with a one-time `?auth_code=`, or with `error` and `error_description`. The access token never appears in a URL. |
| `POST /auth/redeem` | Body `{authCode}`. Exchanges the one-time code for the token. A code lives for 60 seconds and redeems once (`auth.AuthorizationCode` holds its SHA-256 hash). |
| `POST /auth/token-exchange` | RFC 8693: exchanges a token from another provider for one of this host's own. |
| `POST /tenants/switch` | Issues a new token for the caller in another tenant they belong to (the `TenantSwitch` flow). |
| `POST /auth/password` | Lets a user whose password must change set a new one, proving the current one. |
| `POST /auth/logout` | Revokes the presented token. |

The redirect uses three short-lived `HttpOnly` cookies: `fdw_oidc_resume` (the resume token), `fdw_oidc_flow` and `fdw_oidc_return`.

### Package map (server)

```
Fdw.Services.Authentication.Abstractions   IAuthenticationStep, AuthenticationContext, ContextContribution,
                                           ContextElements, ClaimSources, IAuthenticationExecutionStore,
                                           IAuthorizationCodeStore
Fdw.Services.Authentication                AuthenticationRunner, AuthenticationFlowProvider,
                                           AuthenticationStepTypes, DatabaseExecutionStore,
                                           DatabaseAuthorizationCodeStore,
                                           Validation/: AuthenticationServiceTypes, IssuerSchemeSelector,
                                           LocalKeyAuthenticationHandler, ApiKeyAuthenticationHandler
Fdw.Services.TokenManagers.Abstractions    ITokenIssuer, IssuanceRequest, IssuedToken, ITokenRevocationStore
Fdw.Services.TokenManagers                 JwtIssuanceResolver, JwtTokenIssuer,
                                           SecretManagerSigningCredentialProvider, TokenRevocationStore,
                                           TokenManagerTypes (a plain three-phase registrant)
Fdw.Services.Authorization(.Abstractions)  IUserPermissions / UserPermissions: effective roles and permissions
reference-servicetypes                     ReferenceAuthentication.<Step>.Registration (one package per step),
                                           ReferenceAuth.Endpoints (the routes above)
```

## Server Side: Checking a Token

`AuthenticationServiceTypes` holds the validation schemes a host can register:

- `LocalKey`: tokens this host signed.
- `JwtBearer`: tokens from a remote OIDC authority.
- `ApiKey`: opaque `fdx_` credentials, which are looked up rather than read.

`IssuerSchemeSelector` picks the scheme from the token's `iss` claim. It compares the claim to each scheme's `Authority` as an exact ordinal string, so copy the issuer string verbatim between `auth.JwtTokenManager.Issuer` and `auth.JwtAuthentication`. A trailing slash is enough to make a token fail validation.

After the signature, issuer, audience and lifetime checks pass, `LocalKeyAuthenticationHandler` asks `ITokenRevocationStore.IsRevoked(jti, userId, issuedAt)` about every token. A token with no readable `jti` is rejected. If the revocation check itself fails, the token is rejected too. Both failure paths close.

An API key states no permissions. `ApiKeyAuthenticationHandler` resolves the owner's permissions on every request, in the owner's default tenant. A failed resolve denies rather than authenticating with an empty set.

## Client-Side Architecture

### Shared Package: `Web.Http.Authentication`

Core abstractions and DelegatingHandlers used by every .NET client of the API (the `*.Clients` packages, the TUI):

**`IAccessTokenProvider`** -- Returns the current access token. The host supplies the implementation; `Fdw.Services.Identity`'s `ManagedIdentityAccessTokenProvider` is the one in FDW (service-to-service).

**`ITokenRefreshHandler`** -- Coordinates token refresh:
- `CanRefresh` property indicates whether refresh is supported
- `TryRefresh(ct)` attempts to obtain a new token pair

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

## Token Flow: End-to-End

### Password login

```
Client                         reference-api                                  ConfigurationDb / AuthDb
  |  POST /auth/login             |                                                 |
  |  {username, password}         |                                                 |
  |------------------------------>|  AuthenticationRunner.Run(password flow)        |
  |                               |    PasswordCredential -> Subject, Principal ---->|
  |                               |    BakePermissions    -> perm/roles claims ----->|
  |                               |    AuthorizeIssuance  -> Decision               |
  |                               |  terminal check (subject, principal, decision, acr)
  |                               |  JwtTokenIssuer mints RS256 at+jwt              |
  |  {accessToken, tokenType,     |                                                 |
  |   expiresAt,                  |                                                 |
  |   passwordChangeRequired}     |                                                 |
  |<------------------------------|                                                 |
```

The server issues no refresh token. When the access token expires, the client signs in again. Refresh-token issuance is an open decision (FDW-867).

### External provider login (browser)

```
Browser/SPA                     reference-api                      OIDC provider
  |  POST /auth/oidc/redirect     |                                   |
  |  {flowName, returnUrl}        |  flow suspends at OidcRedirect    |
  |------------------------------>|  (context stored, encrypted)      |
  |  {redirectUrl} + cookies      |                                   |
  |<------------------------------|                                   |
  |  navigate to redirectUrl -------------------------------------->  |
  |  <------------------------------------ 302 /auth/oidc/callback?code=
  |  GET /auth/oidc/callback      |  flow resumes: code exchanged,    |
  |------------------------------>|  ResolvePrincipal, BakePermissions,
  |                               |  AuthorizeIssuance, token minted  |
  |  302 returnUrl?auth_code=...  |  token stored under a 60 s code   |
  |<------------------------------|                                   |
  |  POST /auth/redeem {authCode} |                                   |
  |------------------------------>|                                   |
  |  {accessToken, ...}           |                                   |
  |<------------------------------|                                   |
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

`TryRefresh` succeeds only where the host's `ITokenRefreshHandler` can get a new token without the user. For example, `ManagedIdentityAccessTokenProvider` fetches a new service token. Against this API a user's token cannot be refreshed, because no refresh token is issued.

## Security Considerations

### Revocation

Access tokens are stateless JWTs. Revocation has two parts, both in AuthDb and both checked on every request:

- **One token.** `POST /auth/logout` writes the token's `jti` to `auth.RevokedAccessToken` through `ITokenRevocationStore`.
- **Every token of a user.** `ITokenRevocationStore.RevokeAll` writes `auth.UserTokenRevocation`: every token issued to that user before the recorded moment is revoked. It runs when a role is deleted, or when a role's permissions change, for every user holding that role (FDW-875). Their baked permissions are then no longer true.

A role assignment that expires stops counting when permissions are next resolved. Tokens already issued are capped at that expiry when they are minted (see Issuance).

### Short-lived codes, not tokens in URLs

A browser login never carries the access token in a URL. The callback hands back a one-time `auth_code`. The token is stored encrypted under the code's SHA-256 hash for 60 seconds and redeemed once over `POST /auth/redeem`. The return URL is checked against the flow's `ReturnUrls` allow-list before the browser leaves.

### Concurrent Refresh Coordination

Where a client can refresh, `ITokenRefreshCoordinator` stops a burst of simultaneous calls from each starting a refresh. See `DefaultTokenRefreshCoordinator` for the `SemaphoreSlim` and cooldown implementation.

## Extending the Auth Stack

### Adding an authentication step

A new proof (a second factor, a new credential type) is a new step package, not a framework change:

1. Create a `ReferenceAuthentication.<Step>.Registration` project. The `ReferenceAuthStep` dotnet template scaffolds it.
2. Derive from `AuthenticationStepTypeBase` and mark the class `[Implementation]` on `AuthenticationStepTypes`. Declare `Requires`, `Contributes` and `AuthenticationMethods`.
3. Return a `ContextContribution` holding only the elements you declared. If the claims you add stop being true at a known moment, set `ClaimsValidUntil`; the token will not outlive it.
4. Add the step's name to a flow's `auth.AuthenticationFlowStepConfiguration` rows. The flow provider validates the order when it loads, so a missing package or a wrong order fails by name, before anyone logs in.

A host that does not reference a step's package never loads it.

### Custom Token Refresh Coordination

Implement `ITokenRefreshCoordinator` for scenarios the default doesn't cover:

```csharp
// Example: distributed coordination across several client instances
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

`Web.Http.Authentication` does not register an `ITokenRefreshCoordinator` itself; the client host registers the one it uses:
```csharp
services.AddScoped<ITokenRefreshCoordinator, RedisTokenRefreshCoordinator>();
```

## Related Documentation

- `fdw-anatomy/content/authentication.json` -- the source-cited reference for the step pipeline, federation, issuance and provisioning
- [Security Hardening](12-04-Security-Hardening.md) -- OWASP headers, CORS, DB isolation
- [Authorization](12-05-Authorization.md) -- RBAC permissions, roles, endpoint policies
- [Secret Management](12-10-Secret-Management.md) -- SecretManager resolution for the JWT signing key and OIDC client secrets
- [Claim Types](12-15-Claim-Types.md) -- the claims a token carries
