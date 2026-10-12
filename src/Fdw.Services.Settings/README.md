# Fdw.Services.Settings

Application settings as managed configuration rather than a JSON file.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SettingsServiceTypes` | class | ServiceTypeCollection for settings domain service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultSettingsServiceType` | class | Default settings service type. Registers the gateway-backed SettingsConfigurationProvider… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `RoleSettingConfiguration` | class | Database-backed configuration for role-level setting overrides. Generates the table settings.RoleSetting. |
| `ServerSettingConfiguration` | class | Database-backed configuration for server-level settings. Generates the table settings.ServerSetting. |
| `TenantSettingConfiguration` | class | Database-backed configuration for tenant-level setting overrides. Generates the table… |

## Installation

```bash
dotnet add package Fdw.Services.Settings --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
