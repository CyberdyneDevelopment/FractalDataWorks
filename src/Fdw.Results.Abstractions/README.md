# Fdw.Results.Abstractions

The result contract — `IGenericResult`, `IGenericResult<T>`, `IResultCode`, `IResultSeverity` and `ResultCodeBase`, whose catalogue constructor fixes a code's identity as `{prefix}-{number}`.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IResultCategory` | interface | Handling category for a result code — the coarse "what kind of failure, and how is it handled" bucket… |
| `IResultCode` | interface | Interface for typed result codes. |
| `IResultDetails` | interface | Interface for result details that can be formatted into messages. |
| `IResultSeverity` | interface | Interface for result severity levels with logging integration. |
| `IResultStatus` | interface | Interface for result status levels indicating outcome nuance. |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `ResultCategoryBase` | class | Base class for result category implementations using the CRTP pattern. |
| `ResultCodeBase` | class | Base class for result code implementations using the CRTP pattern. |
| `ResultSeverityBase` | class | Base class for result severity implementations using the CRTP pattern. |
| `ResultStatusBase` | class | Base class for result status implementations using the CRTP pattern. |

## Installation

```bash
dotnet add package Fdw.Results.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
