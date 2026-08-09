# DataVault Service Domain

DataVault is FDW's restricted, structured access layer for **user-stored secrets that must be verified but never queried**: passwords, personal access tokens, agent keys, and future payment tokens. The pattern guarantees that stored hash material can never leave the vault — verification happens inside the command, and no command type returns a hash or salt.

## The Four Storage Planes

| Plane | What goes there | Examples |
|---|---|---|
| **DataVault** | User verify-only secrets | Passwords, PATs, agent keys |
| **SecretManager** | System/app secrets | Connection passwords, HMAC keys, RS256 signing keys |
| **DataGateway** | Everything else | OpenIddict tables, `usr.Users`, all domain data |
| **configurationSchema.json** | App-shipped Connection declarations | Only what the app needs to reach ConfigurationDb at boot |

SecretManager secrets (HMAC keys, connection passwords) are **not** vault material. The vault is for user-stored verify-only secrets.

## Architecture Overview

```
Consumer service (e.g. UserCredentialService)
        |
        | resolves vault by name from IDataVaultProvider
        v
IDataVault  (one vault per configured DataVaultConfiguration row)
        |  accepts ONLY IDataVaultCommand — GenericCommand is rejected
        |
        | dispatches to command with DataVaultExecutionContext
        v
IDataVaultCommand.Execute(DataVaultExecutionContext context, CancellationToken)
        |  carries ONE connection + logger — no gateway, no provider
        |  PBKDF2/HMAC verification runs inside the command
        v
IDataConnection  (resolved ONCE from DefaultDataVaultConfiguration.ConnectionId)
```

## Why Connection Is Resolved Once and Never at Request Time

`DefaultDataVault` resolves its connection the first time a command is executed and caches it for the lifetime of the vault instance. It **never** re-looks up a connection by name at request time.

The reason is RLS (Row-Level Security): if the vault queried a connection by name through the standard gateway at request time, a session-context filter (applied per-user in AuthDb) would filter out vault secret rows belonging to other users. The vault must reach secret records for **any** user without RLS filtering — that is possible only when the connection is resolved in system context (startup/first-use), not in a user request context.

Source: `DefaultDataVault.EnsureConnection()` — `DefaultDataVaultConfiguration.ConnectionId` → `IServiceConfigurationProvider<ConnectionConfiguration>.Get(id)` → `IDataConnectionProvider.Get(config)` → cached `IDataConnection`.

### `[ServiceOptionDependency]` on the vault's connection

`IDataConnection` is itself a service-option (`IGenericConnection` is marked `IServiceOption`), so a class that receives one directly would normally trip **FDW044** ("inject the provider, resolve by name — never the service"). The vault is the sanctioned exception: `DefaultDataVaultProvider` resolves the connection **by name in system context** (RLS-free, once) and `CredentialVaultFactory.Create` hands the resolved connection to the **immutable** vault at construction. So the connection constructor parameter carries the per-parameter opt-out:

```csharp
// Source: DataVaultBase / MsSqlDataVaultBase / CredentialVault
protected DataVaultBase(
    [ServiceOptionDependency] IDataConnection connection,  // ← opts this param out of FDW044
    ILogger? logger) { ... }
```

`[ServiceOptionDependency]` (`Fdw.Services.Abstractions`) is the ONLY approved way to accept a service-option directly, and this DataVault chain is its sole current use. Never reach for it to skip injecting a provider — see [TypeCollection Patterns](10-TypeCollection-Patterns.md#service-options-and-the-provider-injection-contract).

## Command Model

### DataVaultCommandBase\<T\>

```csharp
// Inherits DataCommandBase<T> (carries CommandType, ContainerName, PathName)
// Vault commands are STORE-LESS: the vault's single connection IS the store.
// Constructor takes commandType and containerName only.
public abstract class DataVaultCommandBase<TResult> : DataCommandBase<TResult>, IDataVaultCommand<TResult>
{
    protected DataVaultCommandBase(string commandType, string containerName)
        : base(commandType, containerName) { }

    public abstract Task<IGenericResult<TResult>> Execute(
        DataVaultExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

`DataVaultExecutionContext` carries only the vault's single `IDataConnection` and an `ILogger`. Commands never see a gateway, a provider, or any other connection.

### IDataVault Execution Surface

```csharp
// Accepts ONLY vault commands — a general IGenericCommand is rejected
Task<IGenericResult<TResult>> Execute<TResult>(IDataVaultCommand<TResult> command, CancellationToken ct);
Task<IGenericResult>           Execute(IDataVaultCommand command, CancellationToken ct);
```

Passing a plain `IGenericCommand` to `IDataVault` returns a `Failure` result without executing — this is what closes the command surface.

### Narrow Per-Domain Façades

Commands are minted **only** through per-domain façades with `internal` command constructors. No consumer ever `new`s a command directly.

**`ICredentialVaultCommands`** (Users domain, `Services.Users/Commands/`):

```csharp
// Password verbs
IDataVaultCommand<CredentialValidationOutcome> ValidatePassword(Guid userId, string plaintext);
IDataVaultCommand<Guid>                        CreatePassword(Guid userId, string plaintext);
IDataVaultCommand                              RetirePassword(Guid userId);

// SecretType-explicit overloads (for ApiKey, ServiceToken, etc.)
IDataVaultCommand<CredentialValidationOutcome> ValidateCredential(Guid userId, string secretType, string plaintext);
IDataVaultCommand<Guid>                        CreateCredential(Guid userId, string secretType, string plaintext);
IDataVaultCommand                              RetireCredential(Guid userId, string secretType);
```

`CredentialValidationOutcome` carries `IsValid` and `CreatedAt`. It does **not** carry the hash or salt.

**`IPatVaultCommands`** (Credentials.Sql domain, `Services.Credentials.Sql/Commands/`):

```csharp
IDataVaultCommand<PersonalAccessTokenCreatedResult>              Create(Guid userId, string label, DateTime? expiresAt);
IDataVaultCommand<PersonalAccessTokenValidationResult>           Validate(string rawToken);
IDataVaultCommand<IReadOnlyList<PersonalAccessTokenSummary>>     List(Guid userId);
IDataVaultCommand                                                Revoke(Guid userId, Guid tokenId);
IDataVaultCommand                                                RevokeAll(Guid userId);
```

**`IAgentKeyVaultCommands`** (Credentials.Sql domain):

```csharp
IDataVaultCommand<AgentKeyCreatedResult>                   Create(Guid userId, string userName, string label, DateTime? expiresAt);
IDataVaultCommand<IReadOnlyList<AgentKeySummary>>           List(Guid userId);
IDataVaultCommand                                          Revoke(Guid userId, Guid keyId);
```

List commands return metadata-only DTOs. No hash or raw key value appears in any result type.

## Configuration

DataVault uses the standard FDW polymorphic header + typed-body pattern.

### Database Rows

| Table | Purpose |
|---|---|
| `sec.DataVault` | Header: `Id`, `Name`, `ServiceOptionType`, `IsCurrent` |
| `sec.DefaultDataVault` | Typed body: `DataVaultId` (FK → `sec.DataVault.Id`), `ConnectionId` |

`ConnectionId` is the logical `Id` of the `cnx.Connection` row the vault will use. It is resolved once at vault initialization — never re-queried at request time.

### configurationSchema.json Declarations

```json
{
  "DataStores": [
    { "Name": "ConfigurationDb", "Schema": "sec",
      "Containers": [
        { "Name": "DataVault",        "FieldNames": ["RowId","Id","Name","ServiceType","ServiceOptionType","IsCurrent","IsDeleted","Description"] },
        { "Name": "DefaultDataVault", "FieldNames": ["RowId","Id","DataVaultId","ConnectionId","IsCurrent","IsDeleted"] }
      ]
    }
  ]
}
```

### appsettings.json Pointer Sections

The vault is no longer named directly in appsettings. Consumers point at a **credential service** by name (the [Credentials Service Domain](06-08-Credentials-Service-Domain.md)), and the credential policy (vault name, secret manager, HMAC key, environment, token limit) lives in the typed `sec.SqlCredentialService` configuration row — not appsettings.

```json
{
  "Users": {
    "CredentialServiceName": "CredentialService"
  },
  "CredentialsSql": {
    "CredentialServiceName": "CredentialService"
  }
}
```

Both sections carry only a **selector** (which credential service this app uses) — the connections→secret-managers pointer pattern. The HMAC key itself is resolved at runtime through the named `ISecretManagerProvider` — never stored in appsettings.

## Registration

### Three-Phase in Program.cs

```csharp
// Phase 1a: Configure (before builder.Build())
DataVaultServiceTypes.Configure(builder.Services, builder.Configuration, loggerFactory);

// Phase 1b: Register (before builder.Build())
DataVaultServiceTypes.Register(builder.Services, loggerFactory);

// Phase 1c: RegisterDomainServices (before builder.Build()) — add to DI
DataVaultConfigurationProvider.RegisterDomainServices(
    builder.Services,
    dataStoreName: "ConfigurationDb",
    pathName: "sec");

var app = builder.Build();

// Phase 2: Initialize (after builder.Build())
DataVaultServiceTypes.Initialize(app.Services, loggerFactory);
```

`DataVaultServiceTypes` is a `ServiceTypeCollection`. `DefaultDataVaultType` is its built-in `[ServiceTypeOption]`. The vault is consumed by a **credential service** (the [Credentials Service Domain](06-08-Credentials-Service-Domain.md)), which resolves a vault by name through `IDataVaultProvider` and forwards vault commands.

### How PAT / Agent-Key Services Reach the Vault

PAT and agent-key services are **no longer grafted onto `UserServiceTypes`** as a cross-assembly `SqlCredentialServicesType`. They live in `Fdw.Services.Credentials.Sql` as part of the `SqlCredentialServiceType` — the `Sql` option of the **`CredentialServiceTypes`** collection. They keep a direct `IDataVaultProvider` and read their vault name + PAT policy from the typed `SqlCredentialServiceConfiguration` row (selected by `CredentialsSql:CredentialServiceName`). See [Credentials Service Domain](06-08-Credentials-Service-Domain.md) for the full registration story.

`DefaultUserServiceType` continues to register the user-facing services via the same package-reference-as-registration-intent pattern:

```csharp
[ServiceTypeOption(typeof(UserServiceTypes), "Default")]
public sealed class DefaultUserServiceType : UserServiceTypeBase
{
    // Registers: IUserService (Scoped), IUserRoleService (Scoped),
    //            IUserTenantService (Scoped), ICredentialVaultCommands (Singleton),
    //            IUserCredentialService (Scoped), IUserPreferenceService (Scoped)
}
```

## Implementation Guide

### Adding a New Vault Consumer

1. **In your domain package**, create a façade interface and implementation:

```csharp
// MyDomain/Commands/IMySecretVaultCommands.cs
public interface IMySecretVaultCommands
{
    IDataVaultCommand<MyValidationResult> Validate(Guid userId, string plaintext);
    IDataVaultCommand<Guid>              Create(Guid userId, string plaintext);
    IDataVaultCommand                    Retire(Guid userId);
}

// MyDomain/Commands/MySecretVaultCommands.cs
public sealed class MySecretVaultCommands : IMySecretVaultCommands
{
    // mint internal command instances — no consumer sees the concrete command type
    public IDataVaultCommand<MyValidationResult> Validate(Guid userId, string plaintext)
        => new ValidateMySecretCommand(userId, plaintext);
    // ...
}
```

2. **Create concrete commands** with `internal` constructors inheriting `DataVaultCommandBase<T>`:

```csharp
// commandType = logical operation name; containerName = physical table name in the vault
internal sealed class ValidateMySecretCommand
    : DataVaultCommandBase<MyValidationResult>
{
    private readonly Guid _userId;
    private readonly string _plaintext;

    internal ValidateMySecretCommand(Guid userId, string plaintext)
        : base("ValidateMySecret", "MySecretContainer")
    {
        _userId = userId;
        _plaintext = plaintext;
    }

    public override async Task<IGenericResult<MyValidationResult>> Execute(
        DataVaultExecutionContext context, CancellationToken cancellationToken)
    {
        // query record via context.Connection
        // run PBKDF2/HMAC verification here — do NOT return hash/salt
        // return MyValidationResult with IsValid only
    }
}
```

3. **Register in your `ServiceTypeOption`**:

```csharp
services.TryAddSingleton<IMySecretVaultCommands, MySecretVaultCommands>();
services.TryAddScoped<IMySecretService, MySecretService>();
// Why: Scoped to match vault/connection lifetime — never Singleton for vault consumers.
```

4. **Resolve the vault by name** in your service (lazy, fail-loud):

```csharp
var vaultName = _options.Value.MySecretVaultName;
if (string.IsNullOrWhiteSpace(vaultName))
    return GenericResult<T>.Failure(MyLog.VaultNameMissing(_logger));

var vaultResult = await _vaultProvider
    .Get(new DataVaultRequest(null, vaultName), cancellationToken)
    .ConfigureAwait(false);
// vault is cached in a field after first resolution
```

### Adding a New Vault Type Option

Follow the same pattern as `DefaultDataVaultType`:

1. Create `MyDataVaultConfiguration : IDataVaultConfiguration` with `ConnectionId` and any additional properties.
2. Create `MyDataVaultType : DataVaultTypeBase<IDataVault, IDataVaultFactory<IDataVault, DataVaultConfiguration>, DataVaultConfiguration>` decorated with `[ServiceTypeOption(typeof(DataVaultServiceTypes), "MyType")]`.
3. Override `Configure`, `RegisterRequiredServices`, and `RegisterFactory` following `DefaultDataVaultType` as the exemplar.
4. Call `DataVaultConfigurationProvider.Register(Name, typedProvider)` in `RegisterFactory`.
5. If your vault base takes an `IDataConnection` by constructor (resolved by the provider in system context and handed to the immutable vault), mark that parameter `[ServiceOptionDependency]` so it opts out of FDW044 — see [above](#serviceoptiondependency-on-the-vaults-connection).

## Security Rules

- **No command type returns hash or salt.** Verification runs inside the command against the vault's connection; callers receive only a validation outcome.
- **No request-time name lookup.** The vault's connection is resolved once in system context. RLS filtering in AuthDb would otherwise block cross-user secret lookups.
- **SecretManager is not the vault.** HMAC keys, connection passwords, and signing keys belong to `ISecretManagerProvider`. Mixing them would expose system secrets through the vault's narrow-façade surface.
- **Vault consumers are Scoped.** A Singleton holding a Scoped vault consumer is the captive-lifetime bug that broke login. Match the vault/connection lifetime.

## Related Documentation

- [Secret Management](12-10-Secret-Management.md) — SecretManager domain (HMAC keys, signing keys, connection passwords)
- [JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md) — OpenIddict auth flow; vault in the login path
- [Service Domains Overview](06-01-Service-Domains-Overview.md) — ServiceTypeCollection plugin architecture
- [TypeCollection Patterns](10-TypeCollection-Patterns.md) — Cross-assembly registration via ServiceTypeOption
- [Configuration Provider Registration](03-05-Configuration-Provider-Registration-Pattern.md) — Three-phase DI lifecycle
