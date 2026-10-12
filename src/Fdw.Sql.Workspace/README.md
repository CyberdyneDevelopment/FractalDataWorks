# Fdw.Sql.Workspace

A workspace over `.sqlproj` and T-SQL sources.

This package declares 2 interface(s), 1 service/provider type(s).

## Contracts (2)

| Type | Kind | Purpose |
|---|---|---|
| `ISqlWorkspace` | interface | SQL Server Data Tools (.sqlproj) workspace. Holds the parsed TSqlModel plus the on-disk script files,… |
| `ISqlWorkspaceFactory` | interface | — |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `SqlWorkspaceFactory` | class | — |

## Types (5)

| Type | Kind | Purpose |
|---|---|---|
| `ActiveSqlWorkspaceProxy` | class | Singleton wrapper that the MCP host swaps via after each successful load_sqlproject. All members… |
| `SqlWorkspace` | class | Default implementation. Loads .sqlproj by parsing the project XML for &lt;Build Include="*.sql"/&gt;… |
| `NullSqlWorkspace` | class | — |
| `SqlWorkspaceResultCode` | class | — |
| `SqlWorkspaceResultCodes` | class | — |

## Installation

```bash
dotnet add package Fdw.Sql.Workspace --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
