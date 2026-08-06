# Building a Token Manager

This guide walks through the FractalDataWorks **TokenManager** service domain — the provider axis that issues, validates, invalidates, and reads claims from bearer tokens — and shows how to add a new token-manager provider. OpenIddict (RS256 JWT) is the first, reference implementation.

> **Replaces the old auth-server split.** Earlier revisions of FDW modelled authentication as capability-routed `AuthServer` / `AuthenticationServer` / `AuthorizationServer` / `AuthService` collections. That model is **deleted**. There is now **one** provider axis — `TokenManagerTypes` — and one service interface, `ITokenManager`. If you are looking for `IAuthServer`, `AuthServerTypes`, `ITokenIssuanceService`, or `OpenIddictAuthenticationServiceType`, they no longer exist.

## Overview

Authentication in FDW is a standard [service domain](06-01-Service-Domains-Overview.md) built on a [`ServiceTypeCollection`](10-TypeCollection-Patterns.md):

| Piece | Type | Package |
|-------|------|---------|
| Service interface | `ITokenManager` (`IServiceOption`) | `Fdw.Services.TokenManagers.Abstractions` |
| Collection | `TokenManagerTypes` (`[ServiceTypeCollection]`) | `Fdw.Services.TokenManagers` |
| Header config | `TokenManagerConfiguration` (`auth.TokenManager`) | `Fdw.Services.TokenManagers` |
| Config provider | `TokenManagerConfigurationProvider` | `Fdw.Services.TokenManagers` |
| Generic authN service | `AuthenticationService` (`IAuthenticationService`) | `Fdw.Services.TokenManagers` |
| First option | `OpenIddictTokenManagerType` (`[ServiceTypeOption(..., "OpenIddict")]`) | `Fdw.Services.Authentication.OpenIddict` |
| First implementation | `OpenIdTokenManager` | `Fdw.Services.Authentication.OpenIddict` |

The generic `AuthenticationService` knows nothing about any specific provider. It resolves the **one active** token manager by configured name through `IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>` and delegates to it. Swapping OpenIddict for Entra (or any other IdP) is adding one more `[ServiceTypeOption]` — no consumer changes.

## The `ITokenManager` Contract

`ITokenManager` is the four-operation provider seam. It extends `IServiceOption`, so consumers resolve it through its provider by name — never by injecting the interface directly (enforced by [FDW044](13-07-Analyzer-Catalog.md)).

```csharp
// Source: Fdw.Services.TokenManagers.Abstractions/ITokenManager.cs
public interface ITokenManager : IServiceOption
{
    // Issues a token for the grant in the request. Returns the thin ClaimsPrincipal the
    // provider signs in — provider-specific validation (e.g. client_credentials secret) happens here.
    Task<IGenericResult<ClaimsPrincipal>> Issue(TokenIssuanceRequest request, CancellationToken cancellationToken = default);

    // Validates a bearer token (signature, expiry, provider-specific invalidation check) and returns its principal.
    Task<IGenericResult<ClaimsPrincipal>> Validate(string token, CancellationToken cancellationToken = default);

    // Invalidates a previously issued token so later Validate calls reject it.
    Task<IGenericResult> Invalidate(string token, CancellationToken cancellationToken = default);

    // Extracts the claims carried by an already-validated token.
    Task<IGenericResult<ClaimsPrincipal>> ExtractClaims(string token, CancellationToken cancellationToken = default);
}
```

## The Collection: `TokenManagerTypes`

`TokenManagerTypes` is a `[ServiceTypeCollection]` whose `ServiceInterface` is `ITokenManager`. It is **discovered by PlatformServices like every other domain** — no host registers it by hand, and it is **not** `Manual`. Exactly one enabled `auth.TokenManager` row is expected per deployment; the provider resolves it by name.

```csharp
// Source: Fdw.Services.TokenManagers/TokenManagerTypes.cs
[ServiceTypeCollection(
    typeof(TokenManagerTypeBase<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>),
    typeof(ITokenManagerType),
    typeof(TokenManagerTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(ITokenManager),
    ConfigurationType = typeof(TokenManagerConfiguration),
    ProviderType = typeof(DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>),
    ServiceCategory = "TokenManager")]
public partial class TokenManagerTypes : ServiceTypeCollectionBase<
    TokenManagerTypeBase<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>,
    ITokenManagerType>
{
    // Configure(), Register(), Initialize() are source-generated (required by FDW024).
}
```

## The Generic `AuthenticationService`

`AuthenticationService : IAuthenticationService` is provider-agnostic. It verifies first-party credential grants (`password` / `agent_key`) through the credential vault, then resolves and delegates to the active token manager.

```csharp
// Source: Fdw.Services.TokenManagers/AuthenticationService.cs (elided)
public async Task<IGenericResult<ClaimsPrincipal>> Authenticate(
    TokenIssuanceRequest request, CancellationToken cancellationToken = default)
{
    // Only password/agent_key are verified here; every other grant's validation
    // (e.g. client_credentials) is left entirely to the active token manager's Issue.
    if (IsFirstPartyCredentialGrant(request.GrantType))
    {
        var credentialResult = await VerifyCredential(request, cancellationToken).ConfigureAwait(false);
        if (!credentialResult.IsSuccess) return credentialResult.ToNewResult<ClaimsPrincipal>();
    }

    var tokenManager = await ResolveActiveTokenManager(cancellationToken).ConfigureAwait(false);
    if (!tokenManager.IsSuccess) return tokenManager.ToNewResult<ClaimsPrincipal>();

    return await tokenManager.Value!.Issue(request, cancellationToken).ConfigureAwait(false);
}
```

`ResolveActiveTokenManager` reads the config **headers** directly (`TokenManagerConfigurationProvider.Get()`), fails loud if **zero** or **more than one** enabled `auth.TokenManager` row exists, then resolves the service instance through the well-tested `Get(name)` path every other domain uses. No fallback, no defaulted provider.

## The OpenIddict Option (exemplar)

`OpenIddictTokenManagerType` is `[ServiceTypeOption(typeof(TokenManagerTypes), "OpenIddict")]`. It registers everything OpenIddict's engine needs (`AddCore` + `AddServer` + `AddValidation`, the DataGateway-backed stores, the sign-in claim handler) in its `RegisterRequiredServices` — the one registration surface. `OpenIdTokenManager` is the `ITokenManager` implementation.

### Grant routing (`OpenIdTokenManager.Issue`)

| Grant | Behaviour |
|-------|-----------|
| `password` / `agent_key` | Resolve username → active FDW user, verify via `IUserCredentialService`, return a thin principal. `agent_key` adds the `agent` role. |
| `external_identity` | Map the already-validated external principal's `iss`/`sub` to a FDW user via `auth.ExternalIdentity`. |
| `client_credentials` | Resolve `OAUTH_{CLIENTID}` from the header's secret manager, constant-time-compare it to the presented secret, then bake the service principal's effective permissions as `perm` claims. |

Full FDW claim baking (`tenant_id`, `org_id`, `role`, `perm`) for the interactive paths happens in `ProcessSignInClaimsHandler` during OpenIddict's sign-in pipeline — `Issue` returns only the thin identity principal for those paths.

### Client-secret validation

The OAuth **client secret is FDW's secret**, stored in the secret manager under `OAUTH_{CLIENTID}` — never copied onto the OpenIddict application row (`ClientSecretHash` is deliberately `NULL`). Because the app row is never version-on-written, its seeded permissions stay linked. So `OpenIdTokenManager.IssueForClientCredentials` resolves `OAUTH_{CLIENTID}` from the header's configured secret manager and compares it with `CryptographicOperations.FixedTimeEquals`; a missing config, missing secret, or mismatch all fail loud. OpenIddict's own `ValidateClientSecret` event handler is **removed** so the token service owns this check:

```csharp
// Source: OpenIddictTokenManagerType.RegisterOpenIddictComponents (AddServer)
options.RemoveEventHandler(OpenIddictServerHandlers.ValidateClientSecret.Descriptor);
```

### Stateless JWTs and revocation

Access tokens are **stateless RS256 JWTs** — `options.DisableTokenStorage()` means OpenIddict never persists a per-token row, so there is no database round-trip on every request. Revocation therefore has one path:

- `OpenIdTokenManager.Invalidate(token)` reads the token's `jti`, then writes a revocation row to **`auth.RevokedAccessToken`** (with the token's expiry) via `RevokedAccessTokenStore`.
- `OpenIdTokenManager.Validate(token)` verifies the RS256 signature (key resolved from the secret manager) **and** rejects any token whose `jti` has an unexpired row in `auth.RevokedAccessToken`.
- `Logout(subjectId)` revokes the persisted **authorizations** for the subject (and, transitively, their refresh tokens); already-issued access tokens remain valid until expiry unless separately invalidated.

### Signing key

The RS256 signing key and issuer are resolved on demand by `OpenIddictSigningKeyConfigurator` (an `IConfigureOptions<OpenIddictServerOptions>`) through the gateway-backed config providers and the secret manager — the same injected-provider path every other FDW service uses. The key name lives on the header (`SecretManagerName` / `SecretKeyName`); never in appsettings.

## Configuration Tables

The TokenManager domain follows the [polymorphic configuration pattern](03-07-Polymorphic-Configuration-Pattern.md): an identity-only parent header plus one typed body per option.

**Parent — `auth.TokenManager`** (`TokenManagerConfiguration`, identity-only):

| Column | Notes |
|--------|-------|
| `Id`, `Name` | Logical identity + display name |
| `ServiceOptionType` | The `TokenManagerTypes` discriminator, e.g. `"OpenIddict"` |
| `SecretManagerName` | Secret manager that resolves provider secrets (signing key, `OAUTH_*`) |
| `SecretKeyName` | Signing-key secret name within that manager |
| `Description`, tenant/visibility/audit block | — |

**Typed body — `auth.OpenIddictTokenManager`** (`OpenIddictTokenManagerConfiguration`, FK `TokenManagerId → auth.TokenManager.Id`):

| Column | Notes |
|--------|-------|
| `Authority` | Issuer URI (JWT `iss`, discovery doc) |
| `TokenEndpoint` | Absolute or `Authority`-relative; `/connect/token` when empty |
| `AccessTokenLifetime` / `RefreshTokenLifetime` | ISO-8601 durations; applied via `PostConfigure<OpenIddictServerOptions>` |

The typed body is a standalone POCO — it does **not** inherit `TokenManagerConfiguration`. The header provider loads the header, dispatches on `ServiceOptionType` to the typed provider, and sets `header.Configuration = typedBody`.

## Adding a New Token-Manager Provider

Mirror `OpenIddictTokenManagerType`:

1. **Create `Fdw.Services.Authentication.MyProvider`.** Reference `Fdw.Services.TokenManagers.Abstractions`.
2. **Implement `MyTokenManager : ITokenManager`** — the four operations (`Issue` / `Validate` / `Invalidate` / `ExtractClaims`). Do all provider-specific credential/secret validation inside `Issue`.
3. **Create `MyTokenManagerConfiguration : ITokenManagerConfiguration`** as a standalone typed body with `[ManagedConfiguration(ServiceCategory = "TokenManager", ServiceType = "MyProvider")]` and a `TokenManagerId` FK. Put every field your manager reads at runtime on this typed body (parent header stays identity-only).
4. **Create `MyTokenManagerType : TokenManagerTypeBase<...>`** decorated with `[ServiceTypeOption(typeof(TokenManagerTypes), "MyProvider")]`. Override `RegisterRequiredServices` (register your factory, header + typed config providers, and any runtime deps) and `RegisterFactory` (wire the typed provider onto the header provider and register the factory by name).
5. **Nothing in `Program.cs`.** `TokenManagerTypes` is discovered by PlatformServices; the option's `Registration.SourceGenerators` module initializer registers it on package reference. Point the deployment at your provider by seeding a single enabled `auth.TokenManager` row with `ServiceOptionType = 'MyProvider'` plus its typed-body row.

For an external IdP, you can reuse the `external_identity` grant + `auth.ExternalIdentity` mapping rows instead of writing a full credential path — the vault path (`IUserCredentialService.Verify`) is invoked only for `password` / `agent_key`.

## Related Documentation

- [Authentication Architecture](12-11-JWT-Authentication-Architecture.md) — end-to-end server + client token flow
- [Analyzer Catalog](13-07-Analyzer-Catalog.md) — FDW044 (provider injection), FDW024 (phase methods)
- [TypeCollection Patterns](10-TypeCollection-Patterns.md) — `IServiceOption`, ServiceTypeCollection three-phase
- [Secret Management](12-10-Secret-Management.md) — SecretManager resolution for signing keys and `OAUTH_*`
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md) — parent header + typed body
- [Service Domains Overview](06-01-Service-Domains-Overview.md) — ServiceTypeCollection plugin architecture
