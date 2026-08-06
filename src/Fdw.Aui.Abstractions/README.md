# Fdw.Aui.Abstractions

AUI contracts — tools, their descriptions and the confirmation they require.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IAuiAction` | interface | Defines an executable action within the Agent User Interface (AUI). |
| `IAuiProvider` | interface | Defines a provider that contributes semantic metadata to the Agent User Interface (AUI). |

## Models and supporting types (4)

| Type | Kind | Purpose |
|---|---|---|
| `AuiAttribute` | class | Marks a UI component or action as accessible via the Agent User Interface (AUI). |
| `AuiManifest` | class | Represents the semantic map of a UI for an AI agent. Follows Google's A2UI and WebMCP standards. |
| `AuiResource` | class | Represents a readable resource (data) available to an agent within the UI. |
| `AuiTool` | class | Represents a tool (action) available to an agent within the UI. |

## Installation

```bash
dotnet add package Fdw.Aui.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Results.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
