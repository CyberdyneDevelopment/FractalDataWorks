# Fdw.Services.Audit.Abstractions

The audit contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IAuditContextAccessor` | interface | Resolves the for the current call site. Each surface (HTTP endpoint, CLI, background job) registers its… |
| `IAuditService` | interface | Service for recording audit trail entries for entity operations. |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `AuditContext` | class | Caller context for audit operations, extracted at the transport layer. |
| `AuditRecord` | record | Represents a single audit trail record. |

## Installation

```bash
dotnet add package Fdw.Services.Audit.Abstractions --prerelease
```

## Dependencies

`Fdw.Results`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
