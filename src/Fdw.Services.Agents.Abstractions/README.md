# Fdw.Services.Agents.Abstractions

The agent contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IAgentActionService` | interface | Service for managing the AI agent action review queue. |
| `IAgentActionStatusType` | interface | Represents a review status for an agent action. |

## Base types (1)

| Type | Kind | Purpose |
|---|---|---|
| `AgentActionStatusTypeBase` | class | Base class for agent action status types using the CRTP pattern. |

## Models and supporting types (6)

| Type | Kind | Purpose |
|---|---|---|
| `AgentActionRecord` | class | Data record for the agent.AgentAction table. Represents a queued mutating request from an AI agent… |
| `AgentActionStatusTypes` | class | TypeCollection for agent action review status types. Source generator will populate with all discovered… |
| `AgentKeyRecord` | class | Data record for the cfg.AgentKey table. Represents an API key that grants AI agents access to WebMCP… |
| `ApprovedAgentActionStatus` | class | Represents an agent action that has been approved by a human reviewer. |
| `DeniedAgentActionStatus` | class | Represents an agent action that has been denied by a human reviewer. |
| `PendingAgentActionStatus` | class | Represents an agent action that is awaiting human review. |

## Installation

```bash
dotnet add package Fdw.Services.Agents.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
