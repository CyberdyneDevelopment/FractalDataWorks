# Connection Configuration Guide

This guide covers the complete setup required to configure a working MsSql connection that lives as runtime data inside **ConfigurationDb**. Every such connection requires entries across several `conn`, `data`, and `sec` tables, a SQL login with schema permissions, and a secret manager for password resolution.

Before reading this guide, understand the split between the two ways a connection reaches the system:

- **Declared in the shipped schema** — the `ConfigurationDb` and `AuthDb` connections plus the `EnvSecrets` secret manager are declared in `configurationSchema.json`, the JSON file shipped in each entry-point app's content root. The app needs these to *reach* configuration storage in the first place, so they cannot themselves be stored in ConfigurationDb. See [Step 0](#step-0-the-shipped-configuration-schema).
- **Runtime ConfigurationDb rows** — every other connection (`OpsDb`, `NflDb`, `EspnNfl`, `MlbStats`, `LocalFs`, `RoslynWorkspaceLocal`, and any user-defined connection) is a row in ConfigurationDb's `conn.*` / `data.*` tables, created by seed SQL or through the admin write path.

## Configuration Chain Overview

```
SQL Login (master)
  └─ Database User + Schema Permissions (target database)
      └─ conn.Connection (parent record — identity only)
          └─ conn.MsSqlConnection (typed body: server, database, auth type)
              ├─ conn.ConnectionAuthentication (discrete auth columns on the Connection)
              └─ conn.MsSqlConnectionAuthentication (Name/Value KVP rows on the typed body)
                  └─ SecretManager resolves the password at runtime
                      └─ data.DataStore → data.MsSqlDataStore → data.DataPath
                          → data.DataContainer → data.DataContainerField
                            → data.DataContainerKeyField (primary-key markers)
```

The version-on-write pattern means **every child row carries both the parent's logical `Id` and the parent's `RowId`**. The `RowId` is the physical, version-specific primary key (`NEWSEQUENTIALID()`); the `Id` is the durable logical identity. Child tables join on the parent's `RowId` for the current version and store the parent's `Id` for logical lineage. The seed scripts always `SELECT` the parent row and copy both columns — never invent FK values.

---

## Step 0: The shipped configuration schema

`configurationSchema.json` ships in each entry-point app (Reference.Api, Reference.Etl, …). It is loaded into `IConfiguration` at the very start of `Program.cs` via `AddConfigurationGateway<TConnectionFactory, TSecretManager>(filename)`. It declares only the connections, secret managers, and data stores the host needs to reach ConfigurationDb.

The file has three top-level lists under `ConfigurationSchema`: `Connections`, `SecretManagers`, and `DataStores`. A connection's and secret manager's body nests under a `Configuration` object; a connection's auth fields nest under `Configuration.Properties`; a DataStore identifies its type with `TypeId` (not `ServiceOptionType`).

```json
{
  "ConfigurationSchema": {
    "Connections": [
      {
        "Name": "ConfigurationDb",
        "ServiceOptionType": "MsSql",
        "Configuration": {
          "Server": "sql.example.local",
          "Database": "ConfigurationDb",
          "Port": 1433,
          "AuthenticationType": "SqlAuth",
          "TrustServerCertificate": true,
          "Properties": {
            "Username": "fdw_config",
            "SecretManagerName": "EnvSecrets",
            "SecretKeyName": "CONFIG_PASSWORD"
          }
        }
      },
      {
        "Name": "AuthDb",
        "ServiceOptionType": "MsSql",
        "Configuration": {
          "Server": "sql.example.local",
          "Database": "AuthDb",
          "Port": 1433,
          "AuthenticationType": "SqlAuth",
          "TrustServerCertificate": true,
          "Properties": {
            "Username": "fdw_auth",
            "SecretManagerName": "EnvSecrets",
            "SecretKeyName": "AUTH_PASSWORD"
          }
        }
      }
    ],
    "SecretManagers": [
      {
        "Name": "EnvSecrets",
        "ServiceOptionType": "EnvironmentVariable",
        "Configuration": { "Prefix": "FDW_SECRET_" }
      }
    ],
    "DataStores": [
      {
        "Name": "ConfigurationDb",
        "TypeId": "MsSql",
        "Paths": [ /* one entry per schema, each with Containers → Fields → Keys */ ]
      }
    ]
  }
}
```

> The `ConfigurationDb` and `AuthDb` connections are **only** declared here — they are NOT seeded into `conn.*` tables. Everything in the rest of this guide is about the *runtime* connections that DO live in `conn.*` / `data.*`.

---

## Step 1: Create the SQL Login

Logins are server-level principals created in `master`. Use parameterized passwords via sqlcmd variables.

```sql
-- In master database
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'fdw_myservice')
BEGIN
    CREATE LOGIN [fdw_myservice] WITH PASSWORD = N'$(FDW_MYSERVICE_PASSWORD)', CHECK_POLICY = OFF;
    PRINT 'Created login: fdw_myservice';
END
ELSE
BEGIN
    ALTER LOGIN [fdw_myservice] WITH PASSWORD = N'$(FDW_MYSERVICE_PASSWORD)';
    PRINT 'Login already exists: fdw_myservice';
END
```

**Password convention:** `Fdw{Name}Password#` (e.g., `FdwAuthPassword#`) — always the `#` suffix, never `!`.

**Deploy:**
```bash
sqlcmd -S sql.example.local -d master -C \
  -v FDW_MYSERVICE_PASSWORD="FdwMyservicePassword#" \
  -i logins.sql
```

## Step 2: Create Database User and Grant Permissions

Database users map logins to a specific database and grant schema-level access. Grant against the database the connection targets (AuthDb for `fdw_auth`, OpsDb for `fdw_ops`, DataDb for `fdw_nfl`, ConfigurationDb for `fdw_config`).

```sql
-- In the target database, e.g. AuthDb
USE AuthDb;
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'fdw_myservice')
BEGIN
    CREATE USER [fdw_myservice] FOR LOGIN [fdw_myservice];
    PRINT 'Created user: fdw_myservice';
END

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[myschema] TO [fdw_myservice];
PRINT 'Granted CRUD on schema myschema to fdw_myservice';
```

**Permission patterns by role:**

| Role | Permissions |
|------|-------------|
| Full CRUD | `GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[x]` |
| Read-only | `GRANT SELECT ON SCHEMA::[x]` |

**Standard runtime logins** (one login per target database; the password flows through `FDW_SECRET_*`):

| Login | Target database / schema | Permission |
|-------|--------------------------|------------|
| `fdw_config` | ConfigurationDb (all config schemas) | Full CRUD |
| `fdw_config_ro` | ConfigurationDb | SELECT only |
| `fdw_auth` | AuthDb `auth` | Full CRUD |
| `fdw_ops` | OpsDb (`ops`, `etl`, `sched`, `msg`, `dq`) | Full CRUD |
| `fdw_nfl` | DataDb `NflData` / `MlbData` (VM 105) | Full CRUD |

## Step 3: Configure the Secret Manager

Connections resolve passwords at runtime via secret managers. `EnvSecrets` reads environment variables with the `FDW_SECRET_` prefix. It is **declared in `configurationSchema.json`** (Step 0) *and* seeded into `sec.*` so runtime ConfigurationDb connections can resolve passwords without depending on the shipped schema. Both the parent (`sec.SecretManager`) and the typed body (`sec.EnvironmentVariableSecretManager`) are required, joined by `SecretManagerRowId`.

### sec.SecretManager (parent) + sec.EnvironmentVariableSecretManager (typed body)

This mirrors the real seed (`02-seed-cfg-runtime-config.sql`). The typed body `SELECT`s the parent row and copies both `Id` and `RowId`:

```sql
INSERT INTO sec.SecretManager (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), v.Name, v.ServiceOptionType, v.Description
FROM (VALUES
    ('EnvSecrets', 'EnvironmentVariable', 'Environment variable secret manager — resolves FDW_SECRET_* vars')
) v(Name, ServiceOptionType, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM sec.SecretManager x
    WHERE x.Name = v.Name AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
GO

INSERT INTO sec.EnvironmentVariableSecretManager
    (SecretManagerId, SecretManagerRowId, IsEnabled, Prefix, CaseSensitive, Separator, StripPrefix, Target)
SELECT sm.Id, sm.RowId, v.IsEnabled, v.Prefix, v.CaseSensitive, v.Separator, v.StripPrefix, v.Target
FROM (VALUES
    ('EnvSecrets', 1, 'FDW_SECRET_', 0, '_', 1, 'Process')
) v(SecretManagerName, IsEnabled, Prefix, CaseSensitive, Separator, StripPrefix, Target)
JOIN sec.SecretManager sm
    ON sm.Name = v.SecretManagerName AND sm.IsCurrent = 1 AND sm.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM sec.EnvironmentVariableSecretManager x
    WHERE x.SecretManagerId = sm.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

`sec.EnvironmentVariableSecretManager` columns: `IsEnabled`, `Prefix` (`'FDW_SECRET_'`), `CaseSensitive` (`0`), `Separator` (the seeded value is `'_'`, a single underscore), `StripPrefix` (`1`), `Target` (`'Process'`).

**Resolution example:** SecretKeyName `AUTH_PASSWORD` resolves to environment variable `FDW_SECRET_AUTH_PASSWORD`.

### Alternative: MsSql Secret Manager

To store secrets in the database instead of environment variables. `sec.MsSqlSecretManager` has **no `ConnectionString` column** — it uses discrete columns and requires `SecretManagerRowId` (joined to the parent). Actual secret values go in `sec.Secret` (schema `sec`):

```sql
INSERT INTO sec.SecretManager (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), 'MsSqlSecrets', 'MsSql', 'SQL Server-based secret manager'
WHERE NOT EXISTS (
    SELECT 1 FROM sec.SecretManager WHERE Name = 'MsSqlSecrets' AND IsCurrent = 1 AND IsDeleted = 0
);
GO

INSERT INTO sec.MsSqlSecretManager
    (SecretManagerId, SecretManagerRowId, Server, [Database], Port,
     AuthenticationType, Username, SecretKeyName, SecretManagerName,
     TrustServerCertificate, Encrypt, [Schema], TableName, CommandTimeoutSeconds)
SELECT sm.Id, sm.RowId, 'sql.example.local', 'ConfigurationDb', 1433,
       'SqlAuth', 'fdw_secrets', 'SECRETS_PASSWORD', 'EnvSecrets',
       1, 1, 'sec', 'Secret', 30
FROM sec.SecretManager sm
WHERE sm.Name = 'MsSqlSecrets' AND sm.IsCurrent = 1 AND sm.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM sec.MsSqlSecretManager x
    WHERE x.SecretManagerId = sm.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
GO

-- Store actual secret values
INSERT INTO sec.Secret (SecretKey, SecretValue, SecretType, [Description])
VALUES ('NFL_PASSWORD', 'FdwNflPassword#', 'Password', 'Password for fdw_nfl login');
```

`sec.MsSqlSecretManager` columns: `Server`, `[Database]`, `Port`, `AuthenticationType`, `Username`, `SecretKeyName`, `SecretManagerName`, `TrustServerCertificate`, `Encrypt`, `[Schema]` (default `'secrets'`), `TableName` (default `'Secret'`), `CommandTimeoutSeconds`. `sec.Secret` columns: `SecretKey`, `SecretValue`, `Version`, `SecretType`, `Description`, `ExpiresAt`.

## Step 4: Create the Connection

A runtime MsSql connection needs the parent identity row plus its typed body, and authentication is recorded in two places the framework reads.

### conn.Connection (parent — identity only)

```sql
INSERT INTO conn.[Connection] (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), v.Name, v.ServiceOptionType, v.Description
FROM (VALUES
    ('MyServiceDb', 'MsSql', 'Connection to myschema for my service')
) v(Name, ServiceOptionType, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM conn.[Connection] x
    WHERE x.Name = v.Name AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

The real seeds always mint `Id` with `NEWID()` — there are no fixed GUIDs.

| Column | Required | Description |
|--------|----------|-------------|
| `RowId` | (auto) | Version-specific PK, `NEWSEQUENTIALID()` default |
| `Id` | Yes | Durable logical identity (`NEWID()` in seeds) |
| `Name` | Yes | Unique among current rows; referenced in code and DataStore config |
| `ServiceOptionType` | Yes | Discriminator for a registered ConnectionType (`'MsSql'`, `'Http'`, `'FileSystem'`, `'RoslynWorkspace'`, …) |
| `Description` | No | Human-readable description |
| `Environment` | No | Environment filter (NULL = all) |

The parent is identity-only. All server/database fields live on the typed body.

### conn.MsSqlConnection (typed body)

The typed body `SELECT`s the parent and copies both `Id` (→ `ConnectionId`) and `RowId` (→ `ConnectionRowId`). `ConnectionRowId` is the FK enforced by `FK_MsSqlConnection_Connection`:

```sql
INSERT INTO conn.MsSqlConnection
    (ConnectionId, ConnectionRowId, Server, [Database], AuthenticationType,
     ConnectionTimeoutSeconds, CommandTimeoutSeconds,
     TrustServerCertificate, Encrypt, EnableMultipleActiveResultSets)
SELECT c.Id, c.RowId, 'sql.example.local', 'ConfigurationDb', 'SqlAuth', 15, 30, 1, 1, 0
FROM conn.[Connection] c
WHERE c.Name = 'MyServiceDb' AND c.IsCurrent = 1 AND c.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM conn.MsSqlConnection x
    WHERE x.ConnectionId = c.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

| Column | Default | Description |
|--------|---------|-------------|
| `ConnectionId` | - | Parent `conn.Connection.Id` (logical) |
| `ConnectionRowId` | - | Parent `conn.Connection.RowId` (FK, physical) |
| `Server` | `'localhost'` | SQL Server host/IP |
| `Database` | - | Database name |
| `Port` | `1433` | TCP port |
| `InstanceName` | NULL | Named instance |
| `CommandTimeoutSeconds` | `30` | Per-command timeout |
| `ConnectionTimeoutSeconds` | `15` | Connection open timeout |
| `DefaultSchema` | `'dbo'` | Default schema for unqualified names |
| `TrustServerCertificate` | `0` | Set `1` for self-signed certs |
| `Encrypt` | `1` | TLS encryption |
| `EnableConnectionPooling` | `1` | Connection pool |
| `MinPoolSize` | `0` | Minimum pool size |
| `MaxPoolSize` | `100` | Maximum pool size |
| `EnableMultipleActiveResultSets` | `0` | MARS |
| `ApplicationName` | NULL | Appears in SQL Server logs |
| `AutoDiscoverSchema` | `0` | Auto-introspect schema on startup |
| `AuthenticationType` | `'WindowsAuth'` | Auth processor discriminator |

### Authentication: two tables

The MsSql factory reads per-connection auth, and the framework records it in two complementary tables. The seeds populate both.

**`conn.ConnectionAuthentication`** — discrete-column auth on the parent Connection (one current row per connection). Copies the parent `Id`/`RowId`:

```sql
INSERT INTO conn.ConnectionAuthentication
    (ConnectionId, ConnectionRowId, [Type], Username, SecretManagerName, SecretKeyName)
SELECT c.Id, c.RowId, 'SqlAuth', 'fdw_myservice', 'EnvSecrets', 'MYSERVICE_PASSWORD'
FROM conn.[Connection] c
WHERE c.Name = 'MyServiceDb' AND c.IsCurrent = 1 AND c.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM conn.ConnectionAuthentication x
    WHERE x.ConnectionId = c.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

**`conn.MsSqlConnectionAuthentication`** — Name/Value KVP rows hung off the **typed body**. The KVP child loader joins on the parent typed-body row's `RowId`, so **both `MsSqlConnectionId` and `MsSqlConnectionRowId` hold `MsSqlConnection.RowId`** — not `Connection.Id`:

```sql
INSERT INTO conn.MsSqlConnectionAuthentication
    (MsSqlConnectionId, MsSqlConnectionRowId, [Name], [Value], IsCurrent, IsDeleted)
SELECT mc.RowId, mc.RowId, v.KvpName, v.KvpValue, 1, 0
FROM (VALUES
    ('MyServiceDb', 'Type',              'SqlAuth')
   ,('MyServiceDb', 'Username',          'fdw_myservice')
   ,('MyServiceDb', 'SecretManagerName', 'EnvSecrets')
   ,('MyServiceDb', 'SecretKeyName',     'MYSERVICE_PASSWORD')
) v(ConnectionName, KvpName, KvpValue)
JOIN conn.[Connection] c
    ON c.Name = v.ConnectionName AND c.IsCurrent = 1 AND c.IsDeleted = 0
JOIN conn.MsSqlConnection mc
    ON mc.ConnectionId = c.Id AND mc.IsCurrent = 1 AND mc.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM conn.MsSqlConnectionAuthentication x
    WHERE x.MsSqlConnectionId = mc.RowId AND x.[Name] = v.KvpName
    AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

### Authentication KVP names

For a `SqlAuth` connection the four KVP rows are `Type`, `Username`, `SecretManagerName`, `SecretKeyName`:

| KVP `Name` | Example `Value` | Meaning |
|-----------|-----------------|---------|
| `Type` | `SqlAuth` | Matches `AuthenticationType` on the typed body |
| `Username` | `fdw_myservice` | SQL login name |
| `SecretManagerName` | `EnvSecrets` | Which secret manager resolves the password |
| `SecretKeyName` | `MYSERVICE_PASSWORD` | Key looked up in the secret manager |

> Some seeds (e.g. NflDb) intentionally omit the `Type` KVP because `conn.MsSqlConnection.AuthenticationType` already carries it; others (OpsDb, AuthDb) include all four. Either is valid — the factory reads `AuthenticationType` from the typed body first.

## Step 5: Configure the DataStore

A DataStore links a connection to the DataGateway. It also splits into a parent (`data.DataStore`) and an MsSql typed body (`data.MsSqlDataStore`). The DataGateway resolves containers by DataStore name + path + container name.

### data.DataStore (parent) + data.MsSqlDataStore (typed body)

```sql
INSERT INTO data.DataStore (Id, Name, ServiceOptionType, ConnectionId, ConnectionRowId, Description)
SELECT NEWID(), 'MyServiceDb', 'MsSql', c.Id, c.RowId, 'DataStore for myschema tables'
FROM conn.[Connection] c
WHERE c.Name = 'MyServiceDb' AND c.IsCurrent = 1 AND c.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.DataStore x
    WHERE x.Name = 'MyServiceDb' AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
GO

INSERT INTO data.MsSqlDataStore (DataStoreId, DataStoreRowId, DatabaseName, DefaultSchema)
SELECT ds.Id, ds.RowId, 'ConfigurationDb', 'myschema'
FROM data.DataStore ds
WHERE ds.Name = 'MyServiceDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.MsSqlDataStore x
    WHERE x.DataStoreId = ds.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

`data.DataStore` carries both `ConnectionId` (parent `Connection.Id`) and `ConnectionRowId` (FK to `conn.Connection.RowId`).

### data.DataPath

Each DataPath is a schema (for MsSql) or URL segment (for HTTP) within the DataStore. It copies the DataStore's `Id`/`RowId`, and `Path` is NOT NULL:

```sql
INSERT INTO data.DataPath (Id, DataStoreId, DataStoreRowId, Name, [Path], PathType)
SELECT NEWID(), ds.Id, ds.RowId, 'myschema', 'myschema', 'DatabasePath'
FROM data.DataStore ds
WHERE ds.Name = 'MyServiceDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.DataPath x
    WHERE x.DataStoreId = ds.Id AND x.Name = 'myschema' AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

### data.DataPathSegment (optional)

Some paths register explicit ordered segments. Each segment copies the DataPath's `Id`/`RowId`:

```sql
INSERT INTO data.DataPathSegment (Id, DataPathId, DataPathRowId, SegmentValue, Ordinal)
SELECT NEWID(), dp.Id, dp.RowId, 'myschema', 0
FROM data.DataStore ds
JOIN data.DataPath dp ON dp.DataStoreId = ds.Id AND dp.Name = 'myschema'
    AND dp.IsCurrent = 1 AND dp.IsDeleted = 0
WHERE ds.Name = 'MyServiceDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.DataPathSegment x
    WHERE x.DataPathId = dp.Id AND x.Ordinal = 0 AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

### data.DataContainer

One record per table. Copies the DataPath's `Id`/`RowId`:

```sql
INSERT INTO data.DataContainer (Id, DataPathId, DataPathRowId, Name, ContainerType, IsCurrent, IsDeleted)
SELECT NEWID(), p.Id, p.RowId, 'MyTable', 'Table', 1, 0
FROM data.DataStore s
JOIN data.DataPath p ON p.DataStoreId = s.Id AND p.Name = 'myschema'
    AND p.IsCurrent = 1 AND p.IsDeleted = 0
WHERE s.Name = 'MyServiceDb' AND s.IsCurrent = 1 AND s.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.DataContainer x
    WHERE x.DataPathId = p.Id AND x.Name = 'MyTable' AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

`ContainerType` is `'Table'` for MsSql tables and `'Endpoint'` for HTTP REST containers.

### data.DataContainerField

One record per column. Copies the DataContainer's `Id`/`RowId`. **There is no `IsPrimaryKey` column** — primary keys are recorded separately in `data.DataContainerKeyField` (next step):

```sql
INSERT INTO data.DataContainerField
    (Id, DataContainerId, DataContainerRowId, Name, DataType, IsNullable, Ordinal, MaxLength, IsCurrent, IsDeleted)
SELECT NEWID(), c.Id, c.RowId, v.Name, v.DataType, v.IsNullable, v.Ordinal, v.MaxLength, 1, 0
FROM (VALUES
     ('Id',          'uniqueidentifier', CAST(0 AS BIT), 1, CAST(NULL AS INT))
    ,('Name',        'nvarchar',         0, 2, 200)
    ,('Description', 'nvarchar',         1, 3, NULL)
    ,('CreatedAt',   'datetimeoffset',   0, 4, NULL)
) v(Name, DataType, IsNullable, Ordinal, MaxLength)
JOIN data.DataStore s ON s.Name = 'MyServiceDb' AND s.IsCurrent = 1 AND s.IsDeleted = 0
JOIN data.DataPath p ON p.DataStoreId = s.Id AND p.Name = 'myschema'
    AND p.IsCurrent = 1 AND p.IsDeleted = 0
JOIN data.DataContainer c ON c.DataPathId = p.Id AND c.Name = 'MyTable'
    AND c.IsCurrent = 1 AND c.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM data.DataContainerField x
    WHERE x.DataContainerId = c.Id AND x.Name = v.Name AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

| Column | Description |
|--------|-------------|
| `DataContainerId` / `DataContainerRowId` | Logical Id + FK (physical RowId) to `data.DataContainer` |
| `Name` | Column name (must match SQL column exactly) |
| `DataType` | SQL type name: `uniqueidentifier`, `nvarchar`, `int`, `bit`, `datetime2`, `datetimeoffset`, `varchar`, `bigint`, `decimal` |
| `IsNullable` | `1` = NULL allowed, `0` = NOT NULL |
| `IsSystemProvided` | `1` = value supplied by the system (audit columns), `0` = caller-supplied |
| `MaxLength` | String max length (NULL for non-string types) |
| `Precision` | Numeric precision |
| `Scale` | Numeric scale |
| `DefaultValue` | SQL default expression |
| `Ordinal` | Column order (1-based in the seeds) |

### data.DataContainerKeyField

Primary keys (and other key roles) are recorded here, joined to the *field's* `RowId`. The surrogate PK uses `KeyType = 'Surrogate'`:

```sql
INSERT INTO data.DataContainerKeyField (Id, DataContainerFieldRowId, KeyName, KeyType, Ordinal, IsCurrent, IsDeleted)
SELECT NEWID(), dcf.RowId, CONCAT('PK_myschema_', dc.Name), 'Surrogate', 0, 1, 0
FROM data.DataStore ds
JOIN data.DataPath dp ON dp.DataStoreId = ds.Id AND dp.Name = 'myschema'
    AND dp.IsCurrent = 1 AND dp.IsDeleted = 0
JOIN data.DataContainer dc ON dc.DataPathId = dp.Id AND dc.Name = 'MyTable'
    AND dc.IsCurrent = 1 AND dc.IsDeleted = 0
JOIN data.DataContainerField dcf ON dcf.DataContainerId = dc.Id AND dcf.Name = 'Id'
    AND dcf.IsCurrent = 1 AND dcf.IsDeleted = 0
WHERE ds.Name = 'MyServiceDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM data.DataContainerKeyField x
    WHERE x.DataContainerFieldRowId = dcf.RowId AND x.KeyType = 'Surrogate'
    AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

## Step 6: Set Environment Variables

On the application server, set the `FDW_SECRET_` environment variables that `EnvSecrets` resolves.

### systemd service file
```ini
[Service]
Environment=FDW_SECRET_MYSERVICE_PASSWORD=FdwMyservicePassword#
```

### Manual startup
```bash
export FDW_SECRET_MYSERVICE_PASSWORD=FdwMyservicePassword#
./Reference.Api
```

### Loading from a systemd file
```bash
while IFS= read -r line; do
    export "${line#Environment=}"
done < <(grep '^Environment=' /etc/systemd/system/fdw-api.service)
./Reference.Api
```

---

## Using the Connection in Code

### Via DataGateway (preferred)

```csharp
// DataGateway resolves: DataStore("MyServiceDb") → Path("myschema") → Container("MyTable")
var command = Query.From<MyRecord>("MyServiceDb", "myschema", "MyTable")
    .Where(r => r.Name).Equal("test")
    .Build();

var result = await _dataGateway.Execute<IEnumerable<MyRecord>>(command, cancellationToken);
```

### Via ConnectionProvider (low-level)

`IConnectionProvider.Get(name)` returns `Task<IGenericResult<IGenericConnection>>` — await it and check the result:

```csharp
var connectionResult = await _connectionProvider.Get("MyServiceDb", cancellationToken);
if (!connectionResult.IsSuccess) { /* fail loud */ }
var connection = connectionResult.Value;
```

The provider also exposes `Get(Guid id)` and `Get()` (all connections). There is no `Create`, `GetAll`, or `List`.

---

## Complete Example: the AuthDb connection (mirrors the real seed)

`AuthDb` is declared in `configurationSchema.json` (Step 0) so the host can reach it at startup, **and** seeded as a runtime row (`05-seed-authdb-connection.sql`) so DataGateway can route auth-table reads/writes through the standard container path. The runtime seed:

```sql
-- ============================================================================
-- Login (run against master) — fdw_auth scoped to AuthDb only
-- ============================================================================
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'fdw_auth')
    CREATE LOGIN [fdw_auth] WITH PASSWORD = N'$(FDW_AUTH_PASSWORD)', CHECK_POLICY = OFF;
GO

USE AuthDb;
GO
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'fdw_auth')
    CREATE USER [fdw_auth] FOR LOGIN [fdw_auth];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[auth] TO [fdw_auth];
GO

-- ============================================================================
-- Connection parent + typed body (run against ConfigurationDb)
-- ============================================================================
INSERT INTO conn.[Connection] (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), 'AuthDb', 'MsSql', 'AuthDb — authentication credentials and tokens'
WHERE NOT EXISTS (
    SELECT 1 FROM conn.[Connection] WHERE Name = 'AuthDb' AND IsCurrent = 1 AND IsDeleted = 0
);
GO

INSERT INTO conn.MsSqlConnection
    (ConnectionId, ConnectionRowId, Server, [Database], AuthenticationType,
     ConnectionTimeoutSeconds, CommandTimeoutSeconds, TrustServerCertificate, Encrypt, EnableMultipleActiveResultSets)
SELECT c.Id, c.RowId, '$(AuthDbServer)', 'AuthDb', 'SqlAuth', 15, 30, 1, 1, 1
FROM conn.[Connection] c
WHERE c.Name = 'AuthDb' AND c.IsCurrent = 1 AND c.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM conn.MsSqlConnection x WHERE x.ConnectionId = c.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0);
GO

-- Authentication KVP rows (both columns hold MsSqlConnection.RowId)
INSERT INTO conn.MsSqlConnectionAuthentication (MsSqlConnectionId, MsSqlConnectionRowId, [Name], [Value], IsCurrent, IsDeleted)
SELECT mc.RowId, mc.RowId, v.KvpName, v.KvpValue, 1, 0
FROM (VALUES
    ('AuthDb', 'Type',              'SqlAuth')
   ,('AuthDb', 'Username',          'fdw_auth')
   ,('AuthDb', 'SecretManagerName', 'EnvSecrets')
   ,('AuthDb', 'SecretKeyName',     'AUTH_PASSWORD')
) v(ConnectionName, KvpName, KvpValue)
JOIN conn.[Connection] c ON c.Name = v.ConnectionName AND c.IsCurrent = 1 AND c.IsDeleted = 0
JOIN conn.MsSqlConnection mc ON mc.ConnectionId = c.Id AND mc.IsCurrent = 1 AND mc.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM conn.MsSqlConnectionAuthentication x
    WHERE x.MsSqlConnectionId = mc.RowId AND x.[Name] = v.KvpName AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
GO

-- ============================================================================
-- DataStore + typed body + DataPath + DataPathSegment + Container + Fields + Keys
-- (one container per auth table: PersonalAccessToken, RefreshToken, RevokedAccessToken, UserSecret)
-- ============================================================================
INSERT INTO data.DataStore (Id, Name, ServiceOptionType, ConnectionId, ConnectionRowId, Description)
SELECT NEWID(), 'AuthDb', 'MsSql', c.Id, c.RowId, 'AuthDb — credentials + tokens'
FROM conn.[Connection] c
WHERE c.Name = 'AuthDb' AND c.IsCurrent = 1 AND c.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM data.DataStore x WHERE x.Name = 'AuthDb' AND x.IsCurrent = 1 AND x.IsDeleted = 0);
GO

INSERT INTO data.MsSqlDataStore (DataStoreId, DataStoreRowId, DatabaseName, DefaultSchema)
SELECT ds.Id, ds.RowId, 'AuthDb', 'auth'
FROM data.DataStore ds
WHERE ds.Name = 'AuthDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM data.MsSqlDataStore x WHERE x.DataStoreId = ds.Id AND x.IsCurrent = 1 AND x.IsDeleted = 0);
GO

INSERT INTO data.DataPath (Id, DataStoreId, DataStoreRowId, Name, [Path], PathType)
SELECT NEWID(), ds.Id, ds.RowId, 'auth', 'auth', 'DatabasePath'
FROM data.DataStore ds
WHERE ds.Name = 'AuthDb' AND ds.IsCurrent = 1 AND ds.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM data.DataPath x WHERE x.DataStoreId = ds.Id AND x.Name = 'auth' AND x.IsCurrent = 1 AND x.IsDeleted = 0);
GO
-- DataPathSegment, DataContainer, DataContainerField, and DataContainerKeyField
-- follow the patterns in Step 5; see 05-seed-authdb-connection.sql for the full field/key list
-- (UserSecret registers Id as the 'Surrogate' key; RefreshToken/RevokedAccessToken/PersonalAccessToken
--  register their own PK column).

-- ============================================================================
-- Environment variable on the application server:
--   Environment=FDW_SECRET_AUTH_PASSWORD=FdwAuthPassword#
-- ============================================================================
```

---

## Seeded Connections Reference

These are the connections created by the ConfigurationDb seed scripts. `Id` values are minted with `NEWID()` at deploy time — there are no fixed GUIDs. Servers come from sqlcmd variables (`$(OpsDbServer)`, `$(AuthDbServer)`, `$(DataDbServer)`).

| Connection | Type | Database / target | Login | Secret env var | Seed file |
|-----------|------|-------------------|-------|----------------|-----------|
| `OpsDb` | MsSql | OpsDb (`ops`/`etl`/`sched`/`msg`/`dq`) | `fdw_ops` | `FDW_SECRET_OPS_PASSWORD` | `02-seed-cfg-runtime-config.sql` |
| `LocalFs` | FileSystem | `/var/lib/fdw/sources/` | — | — | `02-seed-cfg-runtime-config.sql` |
| `RoslynWorkspaceLocal` | RoslynWorkspace | FDW solution `.slnx` | — | — | `02-seed-cfg-runtime-config.sql` |
| `EspnNfl` | Http | `site.api.espn.com/.../nfl` | — (public) | — | `04-seed-sports-connections.sql` |
| `MlbStats` | Http | `statsapi.mlb.com/api/v1` | — (public) | — | `04-seed-sports-connections.sql` |
| `NflDb` | MsSql | DataDb on VM 105 (`NflData`/`MlbData`) | `fdw_nfl` | `FDW_SECRET_NFL_PASSWORD` | `04-seed-sports-connections.sql` |
| `AuthDb` | MsSql | AuthDb (`auth`) | `fdw_auth` | `FDW_SECRET_AUTH_PASSWORD` | `05-seed-authdb-connection.sql` |

> `ConfigurationDb` and `AuthDb` are also declared in `configurationSchema.json`; `AuthDb` additionally exists as a runtime row (above). `OpsDb` is the only purely runtime-owned MsSql config connection in the base seed set.

The `EnvSecrets` secret manager (`sec.SecretManager` + `sec.EnvironmentVariableSecretManager`) is seeded in `02-seed-cfg-runtime-config.sql`.

---

## Troubleshooting

### Error 18456: Login failed

1. **Login doesn't exist** — create it in `master`.
2. **Database user doesn't exist** — create it in the target database and grant schema permissions.
3. **Wrong password** — check the `FDW_SECRET_*` env var matches the login password.
4. **Secret not resolving** — verify the `conn.MsSqlConnectionAuthentication` KVP rows (and/or `conn.ConnectionAuthentication`) exist with `Username`, `SecretManagerName`, `SecretKeyName`.
5. Never extract connection strings from opened connections; go through DataGateway.

### DataGateway can't find a container

1. **DataStore missing** — `data.DataStore` (+ `data.MsSqlDataStore`) must have a current row with the right `ConnectionId`/`ConnectionRowId`.
2. **DataPath missing** — `data.DataPath` must have a current row for the schema, with `Path` set.
3. **DataContainer missing** — `data.DataContainer` must have a current row for the table.
4. **Name mismatch** — container and field names must match exactly.

### Secret manager not resolving

1. **EnvSecrets** — verify env var `FDW_SECRET_{SecretKeyName}` is set in the process.
2. **MsSqlSecrets** — verify `sec.Secret` has a current row with matching `SecretKey`.
3. **SecretManagerName mismatch** — the value in the auth row must match `sec.SecretManager.Name` exactly.

## See Also

- [Connections Service Domain](06-03-Connections-Service-Domain.md) — Architecture and code patterns
- [DataGateway Pattern](05-01-DataGateway-Pattern.md) — How DataGateway resolves commands
- [Configuration Guide](03-06-Configuration-Guide.md) — Shipped-schema vs runtime vs app configuration
- [JSON-Driven Configuration](03-05-JSON-Driven-Configuration.md) — `configurationSchema.json` shape
- [Database Schema](08-02-Database-Schema.md) — Full schema reference
