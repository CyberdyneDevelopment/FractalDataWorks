# 12-10 Secret Management

This guide covers configuring secret managers for FDW services, with focus on the OpenIddict RS256
signing key resolution and Azure Key Vault integration.

## Overview

FDW services resolve secrets through the **SecretManager** service domain. Secret managers are
configured in the database (`sec.SecretManager` parent + type-specific child tables) and `SecretManagerTypes`
(an ordinary `[ServiceTypeCollection]`) is registered into `PlatformServices` by the generated
`[ModuleInitializer]` — no host registers it by hand. Any service that needs a secret — signing keys,
connection passwords, API keys — references a secret manager by name via `SecretManagerName`/`SecretKeyName`
columns on its own configuration row.

**Dependency order:** SecretManagers -> Connections -> Authentication -> Pipelines. `PlatformServices.Configure`/`Register`/`Initialize`
encode this via each domain's `Group` number — SecretManagers run first so downstream services
(connections, authentication) can resolve their secrets during `Initialize`.

**The platform's primary secret manager is `MsSqlSecrets`** (`ServiceOptionType = 'MsSql'`), not
`EnvSecrets`. `EnvSecrets` (env var `FDW_SECRET_CONFIG_PASSWORD`) resolves **only** the ConfigurationDb
bootstrap login password — the one secret needed before any database read is possible. `MsSqlSecrets`
reads the `sec.Secret` table in ConfigurationDb and resolves every other secret in the system: OpenIddict's
signing key, connection passwords, credential pepper, PAT/HMAC keys, OAuth client secrets, etc. Its own
bootstrap connection is itself resolved via `EnvSecrets`/`CONFIG_PASSWORD` — this is the one deliberate,
documented exception to "everything through MsSqlSecrets" (it has to reach `sec.Secret` before any
`sec.Secret` row is resolvable). See `databases-seed/ConfigurationDb/seed/16b-seed-mssql-secretmanager.sql`.

## Configuration Tables

### Parent: `sec.SecretManager`

Every secret manager has a row in the parent table:

| Column | Type | Purpose |
|--------|------|---------|
| `RowId` | `int identity` | Physical version-on-write PK |
| `Id` | `uniqueidentifier` | Logical identity (durable across versions) |
| `Name` | `nvarchar(256)` | Lookup name (e.g., `MsSqlSecrets`, `EnvSecrets`, `ProductionKeyVault`) |
| `ServiceOptionType` | `nvarchar(100)` | Discriminator: `MsSql`, `EnvironmentVariable`, `AzureKeyVault` |
| `Description` | `nvarchar(500)` | Human-readable description |
| `Environment` | `nvarchar(50)` | Optional environment tag |
| `TenantId` / `VisibilityGroupId` | `uniqueidentifier` | Tenant/visibility scoping |

### Child: `sec.MsSqlSecretManager`

| Column | Type | Purpose |
|--------|------|---------|
| `SecretManagerRowId` | `int` | FK to parent `RowId` |
| `Server` / `Database` / `Port` | — | Connection to the database holding `sec.Secret` (normally ConfigurationDb itself) |
| `AuthenticationType` / `Username` | — | Login credentials |
| `SecretManagerName` / `SecretKeyName` | `nvarchar(256)` | **This manager's own bootstrap password** — resolved through `EnvSecrets`/`CONFIG_PASSWORD` |
| `Schema` / `TableName` | — | Points at `sec.Secret` |

### `sec.Secret` — the actual key-value store this manager reads

`sec.Secret` holds `SecretKey` / `SecretValue` / `SecretType` / `Description` rows. **Values are never
seeded from source-controlled SQL** — `databases-seed/scripts/populate-dev-secrets.sh` generates/derives
each value at deploy time (e.g. `openssl genpkey` for the OpenIddict RSA key) and writes it directly into
the database, idempotently (an existing current secret is left untouched so keys stay stable across
redeploys). The script never prints a secret value.

### Child: `sec.EnvironmentVariableSecretManager`

| Column | Type | Purpose |
|--------|------|---------|
| `SecretManagerRowId` | `int` | FK to parent `RowId` |
| `Prefix` | `nvarchar(100)` | Environment variable prefix (default: `FDW_SECRET_`) |
| `CaseSensitive` | `bit` | Case-sensitive key lookup |
| `Separator` | `nvarchar(10)` | Nested key separator (default: `__`) |
| `StripPrefix` | `bit` | Strip prefix from resolved key name |
| `Target` | `nvarchar(50)` | Variable target: `Process`, `User`, `Machine` |

### Child: `sec.AzureKeyVaultSecretManager`

| Column | Type | Purpose |
|--------|------|---------|
| `SecretManagerRowId` | `int` | FK to parent `RowId` |
| `IsEnabled` | `bit` | Enable/disable without deleting |
| `VaultUri` | `nvarchar(500)` | Key Vault URI (e.g., `https://myvault.vault.azure.net/`) |
| `AuthenticationMethod` | `nvarchar(50)` | `ManagedIdentity`, `ServicePrincipal`, `Certificate`, `DeviceCode` |
| `AzureTenantId` | `nvarchar(256)` | Azure AD tenant ID (service principal / certificate) |
| `ClientId` | `nvarchar(256)` | Application client ID (service principal / certificate) |
| `ClientSecret` | `nvarchar(500)` | Client secret (service principal only) |
| `CertificatePath` | `nvarchar(500)` | Path to .pfx/.p12 file (certificate only) |
| `CertificatePassword` | `nvarchar(500)` | Certificate file password (certificate only) |
| `ManagedIdentityId` | `nvarchar(256)` | Client ID for user-assigned; NULL for system-assigned |
| `Timeout` | `bigint` | Operation timeout in ticks |
| `EnableTracing` | `bit` | Enable distributed tracing |
| `ValidateOnStartup` | `bit` | Validate connectivity during initialization |
| `MaxSecretsPerPage` | `int` | Pagination limit for list operations (max 25) |
| `IncludeDeletedByDefault` | `bit` | Include soft-deleted secrets in list operations |

## How OpenIddict Signing Key Resolution Works

The current auth server is OpenIddict, configured through **`auth.TokenManager`** (parent header,
`[ManagedConfiguration(ServiceCategory = "TokenManager")]`) + **`auth.OpenIddictTokenManager`** (typed
body: `Authority`, `TokenEndpoint`). The header carries `SecretManagerName` / `SecretKeyName` — the same
two columns every other secret-backed config row uses.

`OpenIddictSigningKeyConfigurator` (`Fdw.Services.Authentication.OpenIddict/Hosting/`, an
`IConfigureOptions<OpenIddictServerOptions>`) resolves the key once, when OpenIddict first builds its
server options:

```
1. Load the enabled auth.TokenManager row where ServiceOptionType = 'OpenIddict'.
   Missing → throw (OpenIddict is registered, so a config MUST exist; never fall open to
   OpenIddict's unsigned defaults).
2. Pin options.Issuer from the typed body's Authority (must be an absolute URI).
3. header.SecretManagerName / header.SecretKeyName missing → throw.
4. Resolve the secret manager by name via IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>.
5. Execute GetSecretManagerCommand.Latest(container: null, secretKey: header.SecretKeyName)
   against that manager.
   Failure or missing value → throw (never register unsigned OpenIddict options).
6. RSA.ImportFromPem the resolved PEM value; register it as BOTH the signing credential (RS256)
   and the encryption credential (RSA-OAEP + AES-256-CBC-HMAC-SHA512 — OpenIddict requires at
   least one encryption key even with access-token encryption disabled).
```

**There is no direct-key/no-secret-manager bypass.** `SecretManagerName`/`SecretKeyName` are required —
a `TokenManager` row without them fails loud at startup, consistent with the platform's no-fallback rule.
This is a sync-over-async seam (mirrors `MsSqlConnectionFactory.ResolvePasswordSync`) resolved once via a
short-lived DI scope, not a hosted service or mutable singleton.

## Deployment Scenarios

### Reference deployment: MsSqlSecrets + OpenIddict (the default)

**Seed** (`databases-seed/ConfigurationDb/seed/16b-seed-mssql-secretmanager.sql`):

```sql
-- MsSqlSecrets header + typed body, bootstrapped via EnvSecrets/CONFIG_PASSWORD.
INSERT INTO sec.SecretManager (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), 'MsSqlSecrets', 'MsSql',
       'MsSql secret manager — resolves every non-bootstrap secret from ConfigurationDb sec.Secret'
WHERE NOT EXISTS (SELECT 1 FROM sec.SecretManager WHERE Name = 'MsSqlSecrets' AND IsCurrent = 1 AND IsDeleted = 0);

INSERT INTO sec.MsSqlSecretManager (Id, SecretManagerId, SecretManagerRowId, Server, [Database], Port,
    AuthenticationType, Username, SecretKeyName, SecretManagerName, TrustServerCertificate, Encrypt, [Schema], TableName, CommandTimeoutSeconds)
SELECT sm.Id, sm.Id, sm.RowId, '$(ConfigurationDbServer)', '$(ConfigurationDbDatabase)', 1433,
       'SqlAuth', 'fdw_config', 'CONFIG_PASSWORD', 'EnvSecrets', 1, 1, 'sec', 'Secret', 30
FROM sec.SecretManager sm
WHERE sm.Name = 'MsSqlSecrets' AND sm.IsCurrent = 1 AND sm.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM sec.MsSqlSecretManager x WHERE x.SecretManagerId = sm.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0);
```

**Seed** (`06b-seed-openiddict-server.sql`) — wires the `auth.TokenManager` header to that manager by
name, with the OPENIDDICT_SIGNING_KEY lookup key (a name, not a value):

```sql
INSERT INTO auth.TokenManager (Id, Name, SectionName, ServiceType, ServiceOptionType, SecretManagerName, SecretKeyName)
SELECT NEWID(), 'ApiOpenIddictServer', 'TokenManagers', 'TokenManager', 'OpenIddict', 'MsSqlSecrets', 'OPENIDDICT_SIGNING_KEY'
WHERE NOT EXISTS (SELECT 1 FROM auth.TokenManager WHERE ServiceOptionType = 'OpenIddict' AND IsCurrent = 1 AND IsDeleted = 0);

INSERT INTO auth.OpenIddictTokenManager (Id, TokenManagerId, TokenManagerRowId, Authority, TokenEndpoint)
SELECT NEWID(), h.Id, h.RowId, '$(AuthAuthority)', '/connect/token'
FROM auth.TokenManager h
WHERE h.ServiceOptionType = 'OpenIddict' AND h.IsCurrent = 1 AND h.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM auth.OpenIddictTokenManager x WHERE x.TokenManagerRowId = h.RowId AND x.IsCurrent = 1 AND x.IsDeleted = 0);
```

`$(AuthAuthority)` has no default — a hardcoded preview-slot value goes stale the moment that slot is
destroyed. `deploy-seeds.sh` fails loud if it isn't supplied.

**Populate the actual key value** (`databases-seed/scripts/populate-dev-secrets.sh`) — run once per
environment, never checked into source control:

```bash
FDW_SECRET_CREDENTIAL_PEPPER=... ./scripts/populate-dev-secrets.sh [server] [configDb]
```

It generates a fresh RSA key (`openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048`) and every
other environment secret, and upserts them into `sec.Secret` (existing current values are left untouched,
so redeploying never rotates a live signing key underneath running tokens). No secret value is ever
printed or logged.

### Azure with System-Assigned Managed Identity

Recommended production configuration for Azure-hosted services — no credential management, the managed
identity is provisioned by Azure.

**Prerequisites:**
- App Service / VM / AKS pod has system-assigned managed identity enabled
- Key Vault access policy grants the identity `Get` permission on secrets
- ConfigDb connection uses Entra authentication (no password needed for that hop)

**Seed data:**

```sql
DECLARE @AkvId UNIQUEIDENTIFIER = NEWID();

INSERT INTO sec.SecretManager (Id, Name, ServiceOptionType, Description, Environment)
VALUES (@AkvId, 'ProductionKeyVault', 'AzureKeyVault',
        'Production secrets via Azure Key Vault with system-assigned managed identity', 'Production');

INSERT INTO sec.AzureKeyVaultSecretManager (SecretManagerId, VaultUri, AuthenticationMethod, ValidateOnStartup)
VALUES (@AkvId, 'https://your-vault.vault.azure.net/', 'ManagedIdentity', 1);
```

**Key Vault secret:** create a secret named `openiddict-signing-key` in the vault holding the PEM RSA key.

**Wire the TokenManager to AKV:**

```sql
UPDATE auth.TokenManager
SET SecretManagerName = 'ProductionKeyVault',
    SecretKeyName = 'openiddict-signing-key'
WHERE ServiceOptionType = 'OpenIddict' AND IsCurrent = 1 AND IsDeleted = 0;
```

Since the ConfigDb connection uses Entra auth in Azure (no password), there is no chicken-and-egg problem
— the AKV secret manager is configured through the database like any other service, and it is resolved by
`OpenIddictSigningKeyConfigurator` before the auth server accepts requests.

### Azure with User-Assigned Managed Identity

For environments with multiple managed identities (e.g., shared VMs), specify the identity's client ID:

```sql
INSERT INTO sec.AzureKeyVaultSecretManager (SecretManagerId, VaultUri, AuthenticationMethod, ManagedIdentityId, ValidateOnStartup)
VALUES (@AkvId, 'https://your-vault.vault.azure.net/', 'ManagedIdentity',
        'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx', 1);
```

### Azure with Service Principal

For CI/CD pipelines or cross-tenant access:

```sql
INSERT INTO sec.AzureKeyVaultSecretManager (SecretManagerId, VaultUri, AuthenticationMethod, AzureTenantId, ClientId, ClientSecret, ValidateOnStartup)
VALUES (@AkvId, 'https://your-vault.vault.azure.net/', 'ServicePrincipal',
        'your-tenant-id', 'your-app-client-id', 'your-client-secret', 1);
```

## Configuring via Management UI

Secret managers can also be configured through the Management UI (reference-ui) using the Configuration
pages, which write to the same `sec.SecretManager` and child tables through the domain configuration
provider's `Save()`/`Delete()`. Navigate to **Configuration > Secret Managers**. A change to which secret
manager/key an OpenIddict `TokenManager` row points at takes effect on the **next service restart** — the
signing key is resolved once, at `OpenIddictServerOptions` build time, not re-read per request.

## Consumers of Secret Managers

Any service configuration that has `SecretManagerName` / `SecretKeyName` properties can resolve secrets
through this mechanism. Current consumers:

| Service | Configuration Table | Secret Purpose |
|---------|-------------------|----------------|
| **OpenIddict TokenManager** | `auth.TokenManager` (header) | RS256 signing/encryption key |
| **MsSql Connections** | `conn.ConnectionAuthentication` (header) | SQL Server passwords |
| **PostgreSql Connections** | `conn.ConnectionAuthentication` (header) | PostgreSQL passwords |
| **MsSqlSecrets itself** | `sec.MsSqlSecretManager` | Its own bootstrap password (via `EnvSecrets`) |

## Troubleshooting

### Secret manager not found

```
[Critical] OPENIDDICT-61001: Secret manager 'ProductionKeyVault' not found. Cannot load RS256 signing key.
```

The named secret manager doesn't exist in `sec.SecretManager` with `IsCurrent=1 AND IsDeleted=0`, or the
`ServiceOptionType` doesn't match a registered secret manager type.

**Check:** `SELECT * FROM sec.SecretManager WHERE Name='ProductionKeyVault' AND IsCurrent=1 AND IsDeleted=0;`

### No enabled OpenIddict TokenManager configuration

```
InvalidOperationException: OpenIddict is registered but no enabled OpenIddict token manager
configuration exists in ConfigurationDb (auth.TokenManager with ServiceOptionType='OpenIddict').
```

`06b-seed-openiddict-server.sql` was not run, or the row was soft-deleted. This is the same failure mode
described in that seed script's own comments — it has recurred twice in practice on preview slots after a
DB nuke+rebuild dropped the row.

### Signing key missing / load failed

```
[Critical] OPENIDDICT-61000: Signing key missing: secret manager 'MsSqlSecrets' key 'OPENIDDICT_SIGNING_KEY' returned no value.
[Error]    OPENIDDICT-71000: Signing key load failed: secret manager 'MsSqlSecrets' key 'OPENIDDICT_SIGNING_KEY'. {reason}
```

`sec.Secret` has no current row for `SecretKey = 'OPENIDDICT_SIGNING_KEY'` (or the secret manager itself
failed to resolve). Run `populate-dev-secrets.sh` against the target environment — it is idempotent and
will not disturb an already-present key.

### Signing key PEM parse failed

```
[Error] OPENIDDICT-91000: Signing key PEM parse failed for key 'OPENIDDICT_SIGNING_KEY': {reason}
```

The stored `sec.Secret` value is not a valid PEM-encoded RSA private key (`CryptographicException` /
`ArgumentException` from `RSA.ImportFromPem`). Regenerate with `openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048`.

### AKV connectivity failure at startup

When `ValidateOnStartup=1`, the AKV secret manager tests connectivity during initialization. If it fails,
check:
- `VaultUri` is correct and reachable from the host
- Managed identity is enabled on the Azure resource
- Network security groups allow outbound HTTPS to `*.vault.azure.net`
- `az keyvault secret show --vault-name your-vault --name <secret-name>`
- `az keyvault show --name your-vault --query "properties.accessPolicies"`

## See Also

- [12-11 JWT Authentication Architecture](12-11-JWT-Authentication-Architecture.md) — the full OpenIddict token-issuance architecture
- [10-03 Building Authentication Service](10-03-Building-Authentication-Service.md) — `TokenManagerTypes` / swapping providers
- [12-01 Creating a Server](12-01-Creating-A-Server.md) — `PlatformServices.Configure`/`Register`/`Initialize`
