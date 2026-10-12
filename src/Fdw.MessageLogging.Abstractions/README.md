# Fdw.MessageLogging.Abstractions

The `[MessageLogging]` attribute and the contract its generator emits against — the only sanctioned way to log in FDW.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `MessageLoggingAttribute` | class | Provides information to guide the production of a strongly typed logging method that returns an… |
| `MessageLoggingTypeCodeAttribute` | class | Declares the default TypeCode (the Code-string prefix, e.g. "MSSQL") for every method in the annotated… |

## Installation

```bash
dotnet add package Fdw.MessageLogging.Abstractions --prerelease
```

## Dependencies

`Fdw.Messages`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
