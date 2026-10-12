# Fdw.Aegis

Aegis — the policy gateway surface.

This package declares 1 interface(s), 1 service/provider type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `AegisInjector` | class | The source-agnostic resolve-below-boundary core of the Aegis Gateway. Generalizes… |

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IAegisInjectionTarget` | interface | A pluggable downstream target that hands a resolved to, below the boundary, for exactly the duration of… |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `DeclaredSecretManagerConfigurationProvider` | class | Read-only over the SecretManagers block declared in aegisSchema.json. |

## Types (5)

| Type | Kind | Purpose |
|---|---|---|
| `AegisCommandsOptions` | class | IOptions wrapper around the Commands block loaded from aegisSchema.json. |
| `AegisInjectionOutcome` | class | The sanitized outcome of an injection. Carries only a success flag and a downstream… |
| `AegisLog` | class | MessageLogging for Aegis Gateway operations. Every log message is returned in the result AND logged… |
| `HttpHeaderInjectionTarget` | class | The Phase 1 : injects the resolved secret as an outbound HTTP Authorization: Bearer header. Generalizes… |
| `PreApprovedPolicyEvaluator` | class | Phase 1's : fail-closed, deterministic, no human/agent in the loop. Approves ONLY when the requested… |

## Installation

```bash
dotnet add package Fdw.Aegis --prerelease
```

## Dependencies

`Fdw.Aegis.Abstractions` · `Fdw.Aegis.Configuration` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Services.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
