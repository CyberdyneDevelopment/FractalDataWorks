# JSON-Driven Configuration

`configurationSchema.json` is the shipped Connection / SecretManager / DataStore declarations
that an entry-point app needs to reach configuration storage.

## The shipped schema file: `configurationSchema.json`

Every entry-point app (`Reference.Api`, `Reference.Etl.Server`, `Reference.Scheduler.Server`,
`Reference.UI`) ships a `configurationSchema.json` in its content root. It declares only the
connections the host must already hold to *reach* configuration storage — everything else lives
as runtime data inside ConfigurationDb.

The JSON declares three top-level sections under a `ConfigurationSchema` root:

| Section | Content |
|---|---|
| `Connections` | The connection(s) needed to reach configuration storage. The reference apps declare two: `ConfigurationDb` and `AuthDb`. Per-tenant configuration stores can be added here too. |
| `SecretManagers` | The secret manager(s) used to resolve connection passwords (e.g. `EnvSecrets` reading `FDW_SECRET_*` env vars). |
| `DataStores` | The DataStore shape (paths, containers, fields, keys) for each declared connection — one DataStore per connection. |

Each connection's transport fields (`Server`, `Database`, `Port`, `AuthenticationType`,
`Properties`, …) nest under a `Configuration` object. Each secret manager's settings
(`Prefix`, …) likewise nest under `Configuration`. DataStores use `TypeId` to name the
storage medium.

Every connection NOT declared in the shipped schema (`OpsDb`, `NflDb`, user-defined
connections, datasets, pipelines, schedules) is **runtime data** inside ConfigurationDb's
`conn.*` / `data.*` tables. The domain-specific `*ConfigurationProvider` reads them through
`IConfigurationGateway` after startup. (`AuthDb` is the exception — it is declared in the
shipped schema, not a runtime row, because the login flow needs it before the gateway is live.)

## Example

This mirrors the real `Reference.Api/configurationSchema.json`: two connections
(`ConfigurationDb`, `AuthDb`), one secret manager, and the matching DataStores.

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
        "Configuration": {
          "Prefix": "FDW_SECRET_"
        }
      }
    ],
    "DataStores": [
      { "Name": "ConfigurationDb", "TypeId": "MsSql", "Paths": [ /* schema → container → field/key shapes */ ] },
      { "Name": "AuthDb",          "TypeId": "MsSql", "Paths": [ /* ... */ ] }
    ]
  }
}
```

## The JSON is medium-agnostic

The JSON shape doesn't change based on where configuration is stored. Only the `TypeId` of
the configuration DataStore varies:

- `TypeId="MsSql"` → configuration storage is a SQL Server database (most common).
- `TypeId="Http"` → configuration comes from a REST API.
- `TypeId="File"` → configuration comes from local files.

Whatever the medium, the gateway reads connection details + DataStore shape from the JSON,
then operates against it the same way every other DataStore is operated against.

## Hosting wiring

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
var loggerFactory = builder.AddFrameworkSerilog("Reference.Api");

// 2. ConfigurationGateway — deserialises configurationSchema.json via System.Text.Json so the
//    polymorphic ConnectionConfiguration / SecretManagerConfiguration bodies dispatch to their
//    concrete subtypes on the ServiceOptionType discriminator. The loader is the gateway plus STJ —
//    there is no MsSqlConfigurationSource / SqlServerConfigurationProvider on this path.
builder.Services.AddConfigurationGateway<MsSqlConnectionFactory, EnvironmentVariableSecretManager>(
    "configurationSchema.json");

// Lazy<IDataGateway> must be in DI before any domain provider is registered.
builder.Services.AddSingleton(sp =>
    new Lazy<IDataGateway>(() => sp.GetRequiredService<IDataGateway>()));

// 3. ONE PlatformServices sweep — Configure + Register before Build.
//    Every [ServiceTypeCollection] discovered by the generated module initializer participates
//    (DataGateway, SecretManager, Connection, DataStore, DataSet, …). Each domain's own
//    [ServiceTypeOption] registers the *ConfigurationProvider it depends on (idempotent
//    TryAddSingleton), so there are no hand-written per-domain RegisterDomainServices lines here.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// 4. Initialize phase — after Build, before middleware. ONE sweep, dependency-safe
//    Group order (SecretManager → Connection → DataGateway → … → DataStore → DataSet → rest).
PlatformServices.Initialize(app.Services, loggerFactory);
```

## Related

- [ManagedConfiguration](03-01-ManagedConfiguration.md) — type-side definition
- [Configuration Guide](03-06-Configuration-Guide.md) — shipped-schema vs runtime vs app config, and how each is read
- [Configuration Provider Registration](03-05-Configuration-Provider-Registration-Pattern.md) — the 5-arg `RegisterRequiredServices` pattern
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md) — parent + typed-body for multi-variant domains
