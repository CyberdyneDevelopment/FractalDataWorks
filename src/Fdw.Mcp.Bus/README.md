# Fdw.Mcp.Bus

The MCP message bus.

This package declares 1 model(s).

## Records (1)

| Type | Kind | Purpose |
|---|---|---|
| `McpRequestResponse` | class | Request/response helper for bus-mediated tool invocations. A caller (e.g. the Pidgin SQL participant)… |

## Types (11)

| Type | Kind | Purpose |
|---|---|---|
| `FileEventLogSink` | class | Subscribes to every event on the bus and appends it to an hourly-rotating JSON Lines file. Provides… |
| `InMemoryMcpEventBus` | class | In-process MCP event bus. Holds events in a bounded ring for live subscribers and replay. Durable… |
| `InProcMcpToolSource` | class | In-process tool source — the caller supplies a delegate that handles tool invocations synchronously… |
| `McpBusLog` | class | MessageLogging methods for the MCP event bus. EventId range: 9801-9819. |
| `McpToolEventBusExtensions` | class | Ergonomic helpers for MCP tool servers to publish invocation / result / error events on the bus. Tool… |
| `McpToolSourceHost` | class | Hosted service that owns the lifetime of every registered : starts them at app startup, stops them at… |
| `StdioBridgeMcpToolSource` | class | Stdio-bridge tool source — spawns an external MCP server process and bridges its JSON-RPC stdio to the… |
| `McpEventBusOptions` | class | — |
| `McpEventBusServiceCollectionExtensions` | class | — |
| `McpToolErrorException` | class | — |
| `McpToolSourceServiceCollectionExtensions` | class | — |

## Installation

```bash
dotnet add package Fdw.Mcp.Bus --prerelease
```

## Dependencies

`Fdw.Mcp.Bus.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
