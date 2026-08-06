# Fdw.Hosting.MsSql

Host wiring for an application whose configuration store is SQL Server.

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `HostingMsSqlLog` | class | MessageLogging for FDW MsSql hosting operations. EventId range: 520-540. |

## Installation

```bash
dotnet add package Fdw.Hosting.MsSql --prerelease
```

## Dependencies

`Fdw.Hosting` · `Fdw.Security.Hashing` · `Fdw.Services.Connections.MsSql` · `Fdw.Services.Credentials.Sql` · `Fdw.UI.Components.Blazor.MsSql` · `Fdw.Web.Analytics.Clients`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
