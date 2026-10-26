# Fdw.Aegis.McpServer

The Aegis Gateway's MCP tool surface and host wiring. The executable that hosts it (`aegis-mcp`) lives in reference-servicetypes, which owns the schema, the declared connections and the configuration rows (FDW-1119).

This package declares 1 service/provider type(s).

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `AegisToolService` | class | Exposes the Aegis Gateway's Phase 1 approval + injection pipeline as three MCP tools: list_connections,… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `AegisHostRegistration` | class | Shared DI wiring for the Aegis Gateway MCP host. Phases for the entry point and the non-exposure test suite… |

## Installation

```bash
dotnet add package Fdw.Aegis.McpServer --prerelease
```

## Dependencies

`Fdw.Aegis` · `Fdw.Aegis.Abstractions` · `Fdw.Aegis.Configuration` · `Fdw.Services.Connections.Http` · `Fdw.Services.Data` · `Fdw.Services.SecretManagers`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; edit by hand, nothing regenerates it -->
