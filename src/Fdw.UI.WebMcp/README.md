# Fdw.UI.WebMcp

A page-scoped WebMCP layer, exposing a page's actions as MCP tools.

This package declares 1 model(s).

## Records (1)

| Type | Kind | Purpose |
|---|---|---|
| `WebMcpConfirmationRequest` | class | The confirmation prompt raised before a marked is executed on the agent's behalf. |

## Types (7)

| Type | Kind | Purpose |
|---|---|---|
| `AuiToolExtensions` | class | Projects the AUI layer's tool metadata onto the WebMCP UI layer. |
| `WebMcpBridge` | class | Publishes the tools declared by its child content to the browser's WebMCP model context, so an… |
| `WebMcpPageTool` | class | Declares a single page-scoped WebMCP tool in markup, inside a . |
| `WebMcpRegistrationOutcome` | class | The result of registering a bridge's tools with the browser's WebMCP model context. |
| `WebMcpToolFailure` | class | A single tool the browser refused to register, and the reason it gave. |
| `WebMcpUiLog` | class | MessageLogging for the WebMCP UI layer. EventId ranges: 11097-11102 (informational), 91067-91073 (error). |
| `WebMcpUiTool` | class | A page-scoped tool published to an in-browser AI agent through document.modelContext.registerTool(). |

## Installation

```bash
dotnet add package Fdw.UI.WebMcp --prerelease
```

## Dependencies

`Fdw.Aui.Abstractions` · `Fdw.MessageLogging.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
