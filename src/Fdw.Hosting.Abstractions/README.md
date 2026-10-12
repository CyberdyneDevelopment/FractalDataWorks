# Fdw.Hosting.Abstractions

The contract a host uses to name the connection its configuration lives behind.

## Types (3)

| Type | Kind | Purpose |
|---|---|---|
| `IConfigurationConnectionNameProvider` | interface | Supplies the connection name endpoint base classes use to query configuration data. |
| `DefaultConfigurationConnectionNameProvider` | class | Reads the name from `ConfigurationConnectionOptions` via `IOptionsMonitor`. |
| `ConfigurationConnectionOptions` | class | The options record holding that connection name. |

> **Known defect:** nothing binds `ConfigurationConnectionOptions` to a configuration section, so
> both providers observe its property initializers rather than configured values. See the
> configuration audit before relying on either type.

## Installation

```bash
dotnet add package Fdw.Hosting.Abstractions --prerelease
```

## Dependencies

`Microsoft.Extensions.Options`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)**. Licensed under Apache-2.0.
