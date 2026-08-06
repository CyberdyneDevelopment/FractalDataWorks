# Fdw.Aegis.McpServer

The Aegis MCP server host.

This package declares 1 service/provider type(s).

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `AegisToolService` | class | Exposes the Aegis Gateway's Phase 1 approval + injection pipeline as three MCP tools: list_connections,… |

## Types (2)

| Type | Kind | Purpose |
|---|---|---|
| `AegisHostRegistration` | class | Shared DI wiring for the Aegis Gateway MCP host. Extracted from so the non-exposure test suite can… |
| `Program` | class | Entry point for the Aegis Gateway stdio MCP server (Phase 1: PreApproved commands only,… |

## Installation

```bash
dotnet add package Fdw.Aegis.McpServer --prerelease
```

## Dependencies

`Fdw.Aegis` · `Fdw.Aegis.Abstractions` · `Fdw.Aegis.Configuration` · `Fdw.Services.Connections.Http` · `Fdw.Services.Data` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Registration.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
