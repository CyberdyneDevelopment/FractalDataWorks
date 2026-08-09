# Credentials Service Domain

The Credentials domain is a thin, **named indirection in front of a credential `IDataVault`**. Consumers — the Users domain, the personal-access-token (PAT) service, and the agent-key service — resolve a credential service *by name* and execute vault commands through it, exactly as a connection resolves its secret manager by name. The credential service owns which vault its commands run against; the vault remains the only hash-bearing plane.

This domain replaces the earlier graft where SQL credential services were registered as a cross-assembly `[ServiceTypeOption(typeof(UserServiceTypes), "SqlCredentials")]`. Credentials is now a first-class `ServiceTypeCollection` domain, structurally identical to Connections and DataVault.

## Why a Domain Instead of a UserServiceTypes Graft

`UserServiceTypes` is restricted to the Users domain's own options (`RestrictToCurrentCompilation = true`). Bolting credential storage onto it as a cross-assembly option blurred the domain boundary and was the source of the `IAgentKeyService` boot-resolution fatal. The fix mirrors the **connections → secret-managers** exemplar: a consumer's own configuration carries the *name* of the dependency it needs, and the consumer calls `provider.Get(name)`. Here:

- `UsersServiceOptions.CredentialServiceName` → `ICredentialServiceProvider.Get(name)` → `ICredentialService`.
- The SQL PAT / agent-key services resolve their typed policy row by `CredentialsSql:CredentialServiceName`.

## Architecture Overview

```
Consumer (UserCredentialService / SqlPersonalAccessTokenService / SqlAgentKeyService)
        |
        |  resolves a credential service BY NAME
        v
ICredentialServiceProvider.Get(CredentialServiceRequest)     // Users path
        |
        v
ICredentialService.Execute(IDataVaultCommand)                // canonical vault-command surface
        |
        |  forwards to the configured vault (resolved once by name)
        v
IDataVault  ->  IDataVaultCommand.Execute(DataVaultExecutionContext)   // PBKDF2/HMAC inside the command
```

The credential service surface is deliberately the **canonical `IDataVaultCommand` contract** — there is no separate credential-command hierarchy. A credential service forwards the supplied vault command to its configured vault; verification still happens *inside* the command, so no hash ever crosses the credential-service boundary.

## Surface

### ICredentialService

```csharp
public interface ICredentialService : IGenericService
{
    Task<IGenericResult<TResult>> Execute<TResult>(IDataVaultCommand<TResult> command, CancellationToken ct = default);
    Task<IGenericResult>           Execute(IDataVaultCommand command, CancellationToken ct = default);
}
```

### ICredentialServiceProvider

```csharp
public interface ICredentialServiceProvider : IFdwServiceProvider<ICredentialService>
{
    // Get(name) / Get(id) / Get() / Get(IGenericConfiguration) / Evict are inherited.
    Task<IGenericResult<ICredentialService>> Get(CredentialServiceRequest request, CancellationToken ct = default);
}

public sealed record CredentialServiceRequest(Guid? Id, string? Name)
    : ITypeRequest<Guid, CredentialServiceRequest>;   // empty request (neither Id nor Name) is a structured failure
```

Lookups are `Get(...)` overloads plus the typed `CredentialServiceRequest` — never `GetXxxByName`.

## Configuration

Standard FDW polymorphic header + typed-body pattern.

### Database Rows

| Table | Purpose |
|---|---|
| `sec.CredentialService` | Header: `Id`, `Name`, `ServiceOptionType` (e.g. `Sql`), `IsCurrent` |
| `sec.SqlCredentialService` | Typed body: `CredentialServiceId` (FK → `sec.CredentialService.Id`), `CredentialVaultName`, `SecretManagerName`, `HmacKeySecretName`, `Environment`, `MaxTokensPerUser` |

All credential **policy** lives in the typed body row. Nothing credential-related remains in appsettings — only the pointer.

### appsettings.json — Pointers Only

```json
{
  "Users":        { "CredentialServiceName": "CredentialService" },
  "CredentialsSql": { "CredentialServiceName": "CredentialService" }
}
```

`CredentialsSqlOptions` is a single-property selector — no policy, no defaults. A missing/blank name, or a `Get(name)` miss, fails loud with a MessageLogging error on first credential operation (never a scan, never an "exactly-one" guess).

### configurationSchema.json

`CredentialService` and `SqlCredentialService` containers are declared under the `ConfigurationDb` / `sec` DataStore, mirroring the `DataVault` / `DefaultDataVault` blocks. The save translator intersects POCO ∩ container fields, so every column (including the policy columns and the FK) must be declared.

## Registration — Three-Phase in Program.cs

Inserted **after DataVault** (which it consumes) and **before Authentication / Users** (which consume it):

```csharp
// Phase 1: Configure + Register + RegisterDomainServices (before builder.Build())
CredentialServiceTypes.Configure(builder, loggerFactory);
CredentialServiceTypes.Register(builder.Services, loggerFactory);
CredentialServiceConfigurationProvider.RegisterDomainServices(builder.Services, "ConfigurationDb", "sec");

var app = builder.Build();

// Phase 2: Initialize (after builder.Build()) — scoped provider, factory wiring is per-scope
CredentialServiceTypes.Initialize(app.Services, loggerFactory);
```

`CredentialServiceTypes` sets `RestrictToCurrentCompilation = true` and does **not** set `ConfigurationInterface` (so no lightweight `FactoryProvider` is generated). Its only option — `SqlCredentialServiceType` — lives in the sibling assembly `Services.Credentials.Sql` and registers itself via that package's `Registration.SourceGenerators` module initializer. **The package reference IS the registration intent.**

### SqlCredentialServiceType (the `Sql` option)

```csharp
[ServiceTypeOption(typeof(CredentialServiceTypes), "Sql")]
public sealed class SqlCredentialServiceType
    : CredentialServiceTypeBase<ICredentialService, ICredentialServiceFactory<…>, CredentialServiceConfiguration>
{
    // Configure:                binds List<SqlCredentialServiceConfiguration> + CredentialsSqlOptions (selector)
    // RegisterRequiredServices: SqlCredentialServiceFactory (Scoped — holds the scoped IDataVaultProvider),
    //                           typed body provider (Singleton),
    //                           IPersonalAccessTokenGenerator / Hasher / IPasswordHasher / IAgentKeyVaultCommands (Singleton),
    //                           IPersonalAccessTokenService / IAgentKeyService (Scoped — never Singleton)
    // RegisterFactory:          provider.Register(Name, factory) +
    //                           CredentialServiceConfigurationProvider.Register("Sql", typedProvider)
}
```

The factory is **Scoped** because it holds the scoped `IDataVaultProvider` — a Singleton holding a Scoped dependency is the captive-lifetime bug. The PAT and agent-key services are Scoped for the same reason.

## How Consumers Resolve Credentials

| Consumer | Pointer | Resolution | Vault access |
|---|---|---|---|
| `UserCredentialService` | `Users:CredentialServiceName` | `ICredentialServiceProvider.Get(name)` → `ICredentialService` | through `ICredentialService.Execute` |
| `SqlPersonalAccessTokenService` | `CredentialsSql:CredentialServiceName` | `CredentialServiceConfigurationProvider.Get(name)` → `SqlCredentialServiceConfiguration` | direct `IDataVaultProvider.Get(CredentialVaultName)` |
| `SqlAgentKeyService` | `CredentialsSql:CredentialServiceName` | `CredentialServiceConfigurationProvider.Get(name)` → `SqlCredentialServiceConfiguration` | direct `IDataVaultProvider.Get(CredentialVaultName)` |

`UserCredentialService` routes its credential commands *through* the credential service. The PAT and agent-key services keep a **direct** `IDataVaultProvider` (they need the typed policy to mint PAT commands and resolve the vault) and use the credential service config only as a policy source — never a scan, never an inferred default.

## Logging

`CredentialServiceLog` (EventIds **4547–4599**) covers execution start/success/failure, name-missing, resolve-failed, typed-body-missing, vault-name-missing, vault-resolve-failed, and typed-provider dispatch. PAT/agent-key keep their existing `PersonalAccessTokenLog` (7940–7959) and `AgentKeyLog` (7960–7965) ranges. No secret material (hashes, keys, plaintext) is ever logged — names and ids only.

## Security Rules

- **Vault remains the only hash-bearing plane.** The credential service returns only outcomes; verification happens inside the vault command.
- **Pointer, not policy, in appsettings.** Policy values live in the typed `sec.SqlCredentialService` row; appsettings carries only the credential-service name.
- **No fallback resolution.** Missing/blank name or `Get(name)` miss → fail loud. No magic-string default, no "pick the only one" scan.
- **Scoped consumers.** The factory, PAT service, and agent-key service are Scoped to match the vault/connection lifetime.

## Related Documentation

- [DataVault Service Domain](06-07-DataVault-Service-Domain.md) — the verify-only vault the credential service forwards to
- [Secret Management](12-10-Secret-Management.md) — SecretManager domain (HMAC keys, signing keys, connection passwords)
- [JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md) — OpenIddict auth flow; credentials in the login path
- [Service Domains Overview](06-01-Service-Domains-Overview.md) — ServiceTypeCollection plugin architecture
- [TypeCollection Patterns](10-TypeCollection-Patterns.md) — Cross-assembly registration via ServiceTypeOption
