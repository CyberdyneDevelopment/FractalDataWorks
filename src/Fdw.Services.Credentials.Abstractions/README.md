# Fdw.Services.Credentials.Abstractions

Credential contracts — how a stored credential is described and resolved.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (11)

| Type | Kind | Purpose |
|---|---|---|
| `IAgentKeyVault` | interface | Narrow per-domain interface for the agent-key vault. The raw key is minted by the service edge; the… |
| `ICredentialOutcome` | interface | A credential validation outcome — the result of comparing a presented credential against the vault and… |
| `ICredentialService` | interface | A credential service — a configured, named indirection in front of a credential . Consumers (the Users… |
| `ICredentialServiceConfiguration` | interface | Marker interface for typed credential service body configurations (SqlCredentialServiceConfiguration,… |
| `ICredentialServiceFactory` | interface | Marker interface for credential service factories. |
| `ICredentialServiceFactory<TService, TConfiguration>` | interface | Generic interface for credential service factories with typed configuration. |
| `ICredentialServiceProvider` | interface | Provider for configured credential service instances. |
| `ICredentialServiceType` | interface | Marker interface for credential service type definitions. |
| `ICredentialServiceType<TService, TFactory, TConfiguration>` | interface | Generic interface for credential service type definitions with typed parameters. |
| `ICredentialVault` | interface | Narrow per-domain interface for the password-credential vault. These semantic verbs ARE the access… |
| `IPatVault` | interface | Narrow per-domain interface for the Personal Access Token vault. The raw token is minted by the service… |

## Base types (1)

| Type | Kind | Purpose |
|---|---|---|
| `CredentialOutcomeBase` | class | Base class for credential outcome type options. Concrete options (Match / NoMatch / Expired /… |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `CredentialOutcomes` | class | TypeCollection of credential validation outcomes. The interface and base live here in .Abstractions; the… |
| `CredentialServiceRequest` | record | Typed lookup request for a credential service — identifies the service being requested by logical Id… |

## Installation

```bash
dotnet add package Fdw.Services.Credentials.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Abstractions` · `Fdw.Commands.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.DataVault.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
