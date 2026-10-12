# Fdw.Services.Messaging.Abstractions

The messaging contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IAccessRequestService` | interface | Service for managing access request workflows. |
| `IMessageService` | interface | Service for managing in-system messages with lifecycle tracking. |

## Models and supporting types (5)

| Type | Kind | Purpose |
|---|---|---|
| `AccessRequestPayload` | class | Data transfer object for access requests. |
| `CreateAccessRequest` | class | Request to create an access request. |
| `CreateMessageRequest` | class | Request to create a new in-system message. |
| `MessagePayload` | class | Data transfer object for in-system messages. |
| `MessageQuery` | class | Query parameters for filtering messages. |

## Installation

```bash
dotnet add package Fdw.Services.Messaging.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
