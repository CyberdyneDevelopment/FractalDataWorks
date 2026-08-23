# Managed Identity (`Fdw.Services.Identity`)

FDW-integrated services authenticate to each other, and to external systems, by presenting a
short-lived token they obtained by proving their own identity — not by presenting a static shared
secret that was copied into both ends.

`Fdw.Services.Identity` is the domain that obtains those tokens. Its options are named for the
**mechanism** by which a service proves itself, not for the authorization server that happens to be
answering: both shipped mechanisms are standard OAuth2 grants and work against any server that
implements them. A third mechanism is a new package with a new `[ServiceTypeOption]`, nothing more.

---

## 1. Why this is its own domain

FDW already has four authentication axes. None of them is this one.

| Existing domain | Question it answers | Direction | Who is the subject |
|---|---|---|---|
| `ITokenManager` (TokenManagers) | "Here is a grant — should I mint a token, and is this token still good?" | Inbound | Someone else |
| `IExternalIdentityProvider` (ExternalIdentityProviders) | "This human signed in at an external IdP — who are they here?" | Inbound | A human |
| `ISecretManager` (SecretManagers) | "What value is stored under key K?" | Lookup | Nobody |
| `ICredentialService` / `IAgentKeyVault` / `IPatVault` (Credentials) | "Is this presented credential valid?" | Inbound | Someone else |
| **`IIdentityService` (Identity)** | **"Prove I am me, and give me a token for audience A."** | **Outbound** | **This process** |

The distinguishing property is the last column. In every pre-existing domain the caller is
adjudicating *somebody else's* identity, or fetching a value that has no identity at all. Here the
calling process **is** the subject. That is a different question, so it is a different interface.

### Why not a specialization of `ISecretManager`

`ISecretManager` is a command-pattern facade and `StoreCredentialCommand` / `VerifyCredentialCommand`
/ `RevokeCredentialCommand` already exist, so routing token acquisition through it would compile. It
is still the wrong home:

- **The contract is retrieval, not minting.** `ISecretManager` returns the value stored under a key.
  Nothing is stored here — the token does not exist until it is requested, and requesting it twice
  legitimately yields two different tokens. A "key" would have to be invented to satisfy the shape,
  and an invented key is exactly the kind of made-up value the NO FALLBACKS rule exists to prevent.
- **The contract has no audience.** A managed-identity token is only meaningful *for a named
  audience* — a token minted for the ETL server must not be accepted by the scheduler. Audience,
  scope, and expiry are first-class inputs and outputs here. `ISecretManagerCommand` has nowhere to
  put them, so they would become string-encoded parts of a key, unvalidated.
- **The invalidation semantics differ.** A secret is invalidated *on write*, by whoever wrote it, and
  the cache is tag-invalidated. A token expires *on a clock*, unilaterally, with no write event to
  hang invalidation on. Sharing one cache/invalidation mechanism between them means one of the two
  is wrong.

`ISecretManager` remains involved — the client-credentials option reads its client secret through
`ISecretManager`, exactly as every other FDW component reads a secret. It supplies an input; it is
not the abstraction.

### Why not another `TokenManagerTypes` option

`ITokenManager` is FDW acting as an **authorization server**: `Issue`, `Validate`, `Invalidate`,
`ExtractClaims`, `Logout`, all serving inbound callers. `IIdentityService` is FDW acting as a
**client**, going outbound. The nouns coincide; the direction is opposite, and so is the trust
relationship. `ITokenManager.Issue` returns a `ClaimsPrincipal` for FDW to sign; `IIdentityService`
returns a token some *other* authority already signed and FDW cannot mint.

### Relationship to `ExternalIdentityProviders`

Disjoint. That domain federates **human** login inbound. This one obtains **service** identity
outbound. Both may point at the same Authentik deployment; they share no code path and neither
constrains the other. The XML docs on both say so, because the names alone do not make it obvious.

### Naming note

Three `ManagedIdentity`-named types already exist and **none** of them is this mechanism:
`Fdw.Services.Connections.MsSql`'s `ManagedIdentityConfiguration` (emits
`Authentication=Active Directory Default;` into an MsSql connection string),
`ManagedIdentityAuthenticationMethod` (an `AuthenticationMethods` TypeOption), and
`Fdw.Services.SecretManagers.AzureKeyVault`'s `ManagedIdentityCredentialType`. All three mean
*Azure* Managed Identity specifically. The domain is therefore named `Identity`, and no type in it
is called `ManagedIdentity`.

---

## 2. Shape

```
Fdw.Services.Identity.Abstractions   (netstandard2.0)
├── IIdentityService                 Acquire(IdentityTokenRequest) → IssuedIdentityToken
├── IdentityTokenRequest             audience + scopes (what am I asking for)
├── IssuedIdentityToken              token + type + issuer + audience + scopes + expiry
├── IIdentityServiceConfiguration    typed-body marker
├── IIdentityServiceFactory
├── IIdentityServiceType
└── IIdentityTokenCache              expiry-aware, audience-keyed

Fdw.Services.Identity                (net10.0)
├── IdentityServiceConfiguration     [ManagedConfiguration] parent header (sec.Identity)
├── IdentityServiceTypes             [ServiceTypeCollection] ServiceCategory = "Identity"
├── IdentityServiceTypeBase          CRTP base for options
├── IdentityServiceConfigurationProvider
├── IdentityTokenCache               in-memory, refresh-before-expiry
├── ManagedIdentityAccessTokenProvider   → IAccessTokenProvider  (the outbound seam)
└── Logging/IdentityLog              [MessageLoggingTypeCode("IDENTITY")]

Fdw.Services.Identity.ClientCredentials  (net10.0)
├── ClientCredentialsConfiguration        typed body (sec.ClientCredentialsIdentity)
├── ClientCredentialsIdentityType         [ServiceTypeOption(…, "ClientCredentials")]
├── ClientCredentialsIdentityFactory
└── ClientCredentialsIdentityService      RFC 6749 §4.4 token-endpoint client

Fdw.Services.Identity.JwtAssertion       (net10.0)
├── JwtAssertionConfiguration             typed body (sec.JwtAssertionIdentity)
├── JwtAssertionIdentityType              [ServiceTypeOption(…, "JwtAssertion")]
├── JwtAssertionIdentityFactory
├── JwtAssertionIdentityService           RFC 7523 token-endpoint client
└── Assertions/                           IFederatedAssertionSource + env/file sources
```

One package per mechanism, because that is the unit a host opts into: referencing the package is
what registers the option, so a service that only ever presents a client secret does not carry the
assertion-reading machinery it will never use.

Two options ship deliberately. One option proves nothing about an abstraction — the second is what
demonstrates the shape holds for a mechanism with a genuinely different credential model.

---

## 3. The two mechanisms

### `ClientCredentials` — OAuth2 client credentials (RFC 6749 §4.4)

A service account at the authorization server, plus a registered client. FDW posts
`grant_type=client_credentials` with its client id and secret, and receives a short-lived access
token scoped to the configured audience.

Suited to long-running services (the reference api/etl/scheduler on VM 104) that have somewhere
durable to keep a client secret.

**Be precise about what this buys.** The client secret is *still a static secret at rest* — it moves
from "shared with the peer service" to "shared with the identity provider", read through
`ISecretManager` like any other secret. What changes is meaningful but bounded:

- what crosses the wire on each call is a short-lived token, not the long-lived secret;
- the peer no longer needs a copy of anything, so there is no secret to rotate in two places;
- revocation is central and immediate at the IdP rather than requiring a redeploy of both ends;
- every issuance is logged centrally and attributable to a named service account.

It does **not** achieve zero-secret-at-rest. Only `JwtAssertion` does.

### `JwtAssertion` — signed client assertion (RFC 7523)

The authorization server is configured to trust an external OIDC issuer's signing keys directly. The
workload presents a token that issuer already minted for it — a CI system's per-job OIDC tokens
being the motivating case — and exchanges it for an access token.

There is **no static secret anywhere**: the assertion is minted per job, expires in minutes, and is
bound to the job's identity by the CI system itself. This is the mechanism to prefer wherever the
workload already has a trustworthy issuer.

Its precondition is exactly that: something must already be minting per-workload assertions. A CI
job under a system that issues per-job OIDC tokens has one. A long-running service on VM 104 does not, which is why both options exist rather
than one.

`IFederatedAssertionSource` abstracts where the incoming assertion is read from (an environment
variable for a CI job, a projected file for a Kubernetes-style service-account token) so that
adding a new assertion carrier does not touch the exchange logic.

---

## 4. How a caller uses it

Callers do not use it. That is the design goal.

The domain feeds `IAccessTokenProvider` in `Fdw.Web.Http.Authentication`, which is the seam every
FDW typed HTTP client already goes through via `BearerTokenHandler`. Registering
`ManagedIdentityAccessTokenProvider` means outgoing calls acquire and attach a managed-identity
token with no change at any call site.

The existing implementations of that seam — `BlazorServerAccessTokenProvider` and
`InstanceAccessTokenProvider` — forward the *signed-in user's* token, on purpose, so that downstream
authorization runs as the real user (least privilege). **Managed identity does not replace those and
must not.** It applies where there is no user in the loop: scheduled dispatches, background jobs,
CI-initiated calls, service-initiated reconciliation. Replacing a user-token forward with a service
identity would widen authority, not narrow it.

### Note on the seam's current signature

`IAccessTokenProvider.GetAccessToken` returns `Task<string?>`. A null return cannot say *why* — no
configuration, provider unreachable, credential rejected, all collapse to the same value — which is
the failure mode the always-`IGenericResult` rule exists to prevent. `IIdentityService.Acquire`
returns `IGenericResult<IssuedIdentityToken>` and the bridge logs the distinction on the way out, so
the reason survives into the log even where the seam's own shape cannot carry it. Reshaping the seam
itself is deliberately out of scope here: it is shared with the in-flight ServiceTypeBase work and
belongs in its own change.

---

## 5. Caching

Tokens are cached by `(configuration name, audience, ordered scopes)` and reused until a refresh skew
before expiry. Acquiring a fresh token per outbound call would put the identity provider on the hot
path of every request and make it a hard dependency of every call.

The cache holds tokens only in memory and never persists them. A token is a bearer credential; the
only thing worse than a static shared secret is a static shared secret written somewhere nobody is
watching.

---

## 6. What this does not do

- It does not make the receiving side accept anything. A service that should accept
  externally-issued tokens must register that issuer as an additional JWT bearer issuer alongside
  its own OpenIddict one. That is host configuration, not this domain.
- It does not mint tokens. FDW is the client here; the identity provider is the authority.
- It does not replace user-token forwarding, per §4.
