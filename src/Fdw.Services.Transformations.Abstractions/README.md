# Fdw.Services.Transformations.Abstractions

Transformation-service contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `TransformFieldDescriptor` | record | Declarative descriptor for a single user-facing field on a transformation's configuration. Lets UI… |
| `TransformFieldKinds` | class | Canonical values for . Consumers should compare with StringComparison.Ordinal. |

## Installation

```bash
dotnet add package Fdw.Services.Transformations.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
