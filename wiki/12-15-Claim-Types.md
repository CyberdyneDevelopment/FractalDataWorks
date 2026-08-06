# Claim Types

FDW JWT claims are defined by the **`ClaimDefinitions`** TypeCollection in
`Fdw.Services.Authentication.Abstractions`. It is the single source of truth for
every claim's **wire name** and its **baking metadata** (whether it serialises as a JSON array,
and which token(s) it is written to). Because it is a TypeCollection, *any* assembly — FDW or a
downstream app — can add a new claim by declaring one `[TypeOption]`, with no change to the
claim-baking pipeline.

> The name `ClaimDefinitions` (not `ClaimTypes`) is deliberate — `System.Security.Claims.ClaimTypes`
> is a BCL type and would collide.

## Built-in claims

| Accessor | `.Name` (wire) | `IsArray` | Destination | Meaning |
|---|---|---|---|---|
| `ClaimDefinitions.sub` | `sub` | no | access token | FDW user GUID (OIDC standard; drives RLS via SESSION_CONTEXT) |
| `ClaimDefinitions.tenantId` | `tenantId` | no | access token | Active tenant |
| `ClaimDefinitions.orgId` | `orgId` | no | access token | Org within the tenant |
| `ClaimDefinitions.roles` | `roles` | **yes** | access token | Assigned role names (always a JSON array) |
| `ClaimDefinitions.perm` | `perm` | no | access token | One claim per resolved permission |
| `ClaimDefinitions.crossTenant` | `crossTenant` | no | access token | `"true"` for a cross-tenant token (RLS Mode 2) |

Claim wire names are **camelCase**, not snake_case. This is a hard requirement: the TypeCollection
source generator turns the `[TypeOption]` name into the static accessor *and* into `.Name`, so the
name must be a valid C# identifier (`tenantId`, not `tenant_id`). `sub`/`roles`/`perm` are already
valid identifiers and are unchanged.

## Reading a claim

Always go through `ClaimDefinitions` — never hard-code the string:

```csharp
using Fdw.Services.Authentication.Abstractions;

var tenant = principal.FindFirstValue(ClaimDefinitions.tenantId.Name);   // "tenantId"
var def    = ClaimDefinitions.ByName("tenantId");                        // lookup by wire name
var all    = ClaimDefinitions.All();                                     // enumerate every claim
```

`ByName(name)` returns the **`ClaimDefinitions.NotFound`** sentinel for an unknown name — never
`null`. Compare with `def != ClaimDefinitions.NotFound`, not `def is null`.

## Adding a new claim type

Add **one file** — a `[TypeOption]` on `ClaimDefinitions` that extends `ClaimDefinitionBase`.

```csharp
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>The user's department code.</summary>
[TypeOption(typeof(ClaimDefinitions), "department")]
public sealed class DepartmentClaim : ClaimDefinitionBase
{
    public DepartmentClaim()
        : base(
            id: 7,                                   // unique within the collection
            name: "department",                      // wire name == accessor; must be a valid identifier
            isArray: false,                          // true → serialised as a JSON array
            TokenDestinations.AccessToken)           // one or more of TokenDestinations.*
    { }
}
```

Constructor parameters (`ClaimDefinitionBase`):

| Param | Purpose |
|---|---|
| `id` | Unique `int` within the collection (the generator errors on a duplicate). |
| `name` | The JWT claim's wire name. Must be a valid C# identifier — this is also the static accessor (`ClaimDefinitions.department`) and `.Name`. |
| `isArray` | `true` ⇒ the claim is always written as a JSON array, even for a single value (like `roles`). |
| `destinations` | `params string[]` of `TokenDestinations.AccessToken` / `TokenDestinations.IdentityToken`. Controls which issued token(s) carry the claim. |

That's the whole registration. Because `ClaimDefinitions` is a TypeCollection, the new option is
discovered automatically: `ClaimDefinitions.department`, `ClaimDefinitions.ByName("department")`,
and `ClaimDefinitions.All()` all include it on the next build.

### Adding one from a downstream app (no FDW change)

Declare the same `[TypeOption(typeof(ClaimDefinitions), "…")]` class in your own assembly. The
assembly must reference `Fdw.Collections.SourceGenerators` (as every FDW package does —
`OutputItemType="Analyzer" ReferenceOutputAssembly="false"`) so the option is registered into the
collection at module load. No FDW edit, no recompile of FDW.

## How baking uses the definition

`ProcessSignInClaimsHandler` bakes the resolved principal's claims into the access-token principal
**generically** — it does not hard-code any claim. For each resolved claim type it looks up the
definition and serialises accordingly:

```csharp
var definition = ClaimDefinitions.ByName(claimType);
if (definition != ClaimDefinitions.NotFound && definition.IsArray)
    // one JSON-array claim (JsonClaimValueTypes.JsonArray), destinations from definition.Destinations
else
    // one scalar claim per value; destinations from definition.Destinations,
    // or access-token by default when the claim is not in the catalog
```

Consequences:

- **A registered claim controls its own serialisation.** Set `isArray: true` and it is emitted as a
  JSON array; set its `destinations` and it lands in the right token(s).
- **An unregistered claim still bakes** — as a scalar on the access token (a sane default so apps can
  pass through claims without registering them). Register it only when you need array semantics or a
  non-default destination.
- `sub` is skipped during baking (OpenIddict sets it from the principal's subject).

## Producing a claim's value

Registering a `ClaimDefinition` tells the pipeline **how** to serialise a claim; it does not by
itself put a value on the token. The value must be present on the principal that
`ProcessSignInClaimsHandler` bakes from. For the built-ins, `DefaultPrincipalResolver` produces the
values (sub/tenant/org/roles/perm) from the user's tenant, roles, and resolved permissions. To emit
a **new** claim's value you either:

1. add it on the inbound principal (e.g. an external-identity claim that should pass through), or
2. contribute it where the principal is built (the resolver / issuance path),

and the generic baking will serialise it per its `ClaimDefinition`.

## Related

- [JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md)
- [Authorization](12-05-Authorization.md) — policies consume the `perm` claim
- [TypeCollections Overview](04-01-Overview.md) — the `[TypeCollection]`/`[TypeOption]` mechanism
