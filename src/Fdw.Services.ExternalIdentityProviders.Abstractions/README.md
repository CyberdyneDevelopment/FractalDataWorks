# Fdw.Services.ExternalIdentityProviders.Abstractions

Contracts for external identity providers.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (12)

| Type | Kind | Purpose |
|---|---|---|
| `IExternalIdentityProvider` | interface | Provider axis for "a thing that validates an external IdP token and produces a ". One implementation… |
| `IExternalIdentityProviderConfiguration` | interface | Marker interface for typed external-identity-provider body configurations (e.g. an OIDC-specific… |
| `IExternalIdentityProviderFactory` | interface | Marker interface for external identity provider factories. Mirrors ITokenManagerFactory: the non-generic… |
| `IExternalIdentityProviderFactory<TService, TConfiguration>` | interface | Generic interface for external identity provider factories with typed configuration. |
| `IExternalIdentityProviderType` | interface | Non-generic interface for external identity provider service types. |
| `IExternalIdentityProviderType<TService, TConfiguration, TFactory>` | interface | Interface for external identity provider service types. Mirrors ITokenManagerType's generic/non-generic… |
| `IExternalIdentityProvisioner` | interface | Just-in-time provisioning mechanism consulted by the external-identity issuance path when an… |
| `IExternalIdentityProvisionerConfiguration` | interface | Marker interface for typed external-identity-provisioner body configurations (e.g. the Chained… |
| `IExternalIdentityProvisionerFactory` | interface | Marker interface for external identity provisioner factories. Mirrors IExternalIdentityProviderFactory:… |
| `IExternalIdentityProvisionerFactory<TService, TConfiguration>` | interface | Generic interface for external identity provisioner factories with typed configuration. |
| `IExternalIdentityProvisionerType` | interface | Non-generic interface for external identity provisioner service types. |
| `IExternalIdentityProvisionerType<TService, TConfiguration, TFactory>` | interface | Interface for external identity provisioner service types. Mirrors IExternalIdentityProviderType's… |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `ExternalIdentityProviderSummaryDto` | class | The login-discovery view of an active external identity provider — the minimal, public subset a login… |

## Installation

```bash
dotnet add package Fdw.Services.ExternalIdentityProviders.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
