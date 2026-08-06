# Fdw.Services.SecretManagers.AzureKeyVault

The Azure Key Vault secret store.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `AzureCredentialTypes` | class | TypeCollection for Azure Key Vault credential types. Supports: ManagedIdentity, ServicePrincipal,… |

## Options (6 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CertificateCredentialType` | class | Azure credential type that uses certificate-based authentication. |
| `DeviceCodeCredentialType` | class | Azure credential type that uses device code authentication. |
| `ManagedIdentityCredentialType` | class | Azure credential type that uses managed identity authentication. |
| `ServicePrincipalCredentialType` | class | Azure credential type that uses service principal (client secret) authentication. |
| `AzureKeyVaultConfigurationCommand` | class | — |

## Installation

```bash
dotnet add package Fdw.Services.SecretManagers.AzureKeyVault --prerelease
```

## Dependencies

`Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Services` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
