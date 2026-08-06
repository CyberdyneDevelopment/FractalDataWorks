# Fdw.Validation

Validation helpers and the guard methods FDW services use to reject bad input with a coded failure rather than an exception.

## Types (5)

| Type | Kind | Purpose |
|---|---|---|
| `FdwConfigurationValidator<T>` | class | Base validator for configuration classes that integrates FluentValidation with for startup validation. |
| `FdwValidationRules` | class | Reusable validation rules for the Fdw framework. |
| `FdwValidator<T>` | class | Base validator for API request validation that provides common FDW validation rules. |
| `ValidationResultExtensions` | class | Extension methods to bridge FluentValidation results into the FDW result pattern. |
| `ValidationServiceExtensions` | class | Extension methods for registering FDW validation services. |

## Installation

```bash
dotnet add package Fdw.Validation --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results` · `Fdw.Validation.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
