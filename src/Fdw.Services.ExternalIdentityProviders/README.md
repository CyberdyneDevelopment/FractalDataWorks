# Fdw.Services.ExternalIdentityProviders

External identity providers — the login side of auth, distinct from the token managers that issue FDW's own tokens.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ExternalIdentityProviderTypes` | class | Collection of external identity provider service types. Structurally copies TokenManagerTypes and is… |
| `ExternalIdentityProvisionerTypes` | class | Collection of external identity provisioner service types. Structurally copies and is swept by… |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ChainedExternalIdentityProvisionerType` | class | Chained ServiceTypeOption. Registers the header + typed-body gateway-backed configuration providers and… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `ChainedExternalIdentityProvisionerConfiguration` | class | Typed-body configuration for the Chained external-identity-provisioner TypeOption. Standalone POCO —… |
| `ChainedProvisionerStepConfiguration` | class | Ordered child of — one row per sibling sec.ExternalIdentityProvisioner the chain delegates Provision to,… |
| `ExternalIdentityProviderConfiguration` | class | Header configuration for external identity provider services representing the… |
| `ExternalIdentityProviderTypeBase<TService, TConfiguration, TFactory>` | class | Base class for external identity provider service type definitions. Structurally copies… |
| `ExternalIdentityProvisionerBindingConfiguration` | class | Flat selector row binding a (, ) pair to the named sec.ExternalIdentityProvisioner that should handle… |
| `ExternalIdentityProvisionerConfiguration` | class | Header configuration for external identity provisioner services representing the… |
| `ExternalIdentityProvisionerTypeBase<TService, TConfiguration, TFactory>` | class | Base class for external identity provisioner service type definitions. Structurally copies (3-parameter… |

## Installation

```bash
dotnet add package Fdw.Services.ExternalIdentityProviders --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.ExternalIdentityProviders.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
