# Fdw.Mcp.Bus.Abstractions

MCP bus contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (4)

| Type | Kind | Purpose |
|---|---|---|
| `IMcpEventBus` | interface | MCP event bus contract. Publishers (MCP servers, dispatchers, participants) emit events; sinks (stdio,… |
| `IMcpToolSource` | interface | Bridges an MCP tool implementation onto an . Subscribes to mcp/{ServerName}/*/invoke events and produces… |
| `IMcpToolSourceKind` | interface | TypeOption selector for how external MCP tool implementations are joined to the bus (in-process library,… |
| `IViewIntent` | interface | Per-event directive controlling whether view-bound sinks (e.g. the Pidgin canvas) project this event.… |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `McpToolSourceKindBase` | class | — |
| `McpToolSourceTypes` | class | — |
| `ViewIntentBase` | class | — |
| `ViewIntents` | class | — |

## Models and supporting types (10)

| Type | Kind | Purpose |
|---|---|---|
| `McpEvent` | record | A single event flowing through the MCP bus. Events are immutable, totally ordered per bus instance via ,… |
| `McpEventDraft` | record | A publisher-supplied event payload that the bus will assign an and to on . |
| `McpTopicPattern` | class | Matches MCP topic strings against glob-style patterns. * matches a single segment; ** matches zero or… |
| `McpTopics` | class | Topic-naming conventions for MCP events on the bus. All MCP servers publish to topics under… |
| `Compare` | class | — |
| `Ghost` | class | — |
| `InProcKind` | class | — |
| `Silent` | class | — |
| `StdioBridgeKind` | class | — |
| `Update` | class | — |

## Installation

```bash
dotnet add package Fdw.Mcp.Bus.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results.Abstractions` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
