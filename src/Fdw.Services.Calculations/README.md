# Fdw.Services.Calculations

The calculations service domain: calculation entities, their inputs and their execution.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `CalculationEntityTypes` | class | Mutable registry of calculation entity types. Supports cross-assembly registration of types decorated… |
| `CalculationOperationTypes` | class | TypeCollection for composable calculation operations. Source generator discovers all types decorated… |
| `CalculationServiceTypes` | class | ServiceTypeCollection for calculation domain service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultCalculationServiceType` | class | Default calculation service type that registers calculation entity services (ICalculationEntityService,… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `CalculationEntityConfiguration` | class | Aggregate configuration for the calc.CalculationEntity table — the header plus its composed child… |
| `CalculationStepConfiguration` | class | ManagedConfiguration for the calc.CalculationStep table. Represents a single composable step within a… |
| `CalculationStepFieldConfiguration` | class | Relational replacement for CalculationStep.GroupByFields / OrderByFields. Each row references a… |
| `CalculationStepOperandConfiguration` | class | ManagedConfiguration for the calc.CalculationStepOperand table. Represents a single operand bound to a… |
| `FormulaCalculationConfiguration` | class | Configuration for a Formula calculation entity. Carries the formula language discriminator, the formula… |
| `WindowedCalculationConfiguration` | class | Configuration for a Windowed calculation entity. Specifies the target field, the window function to… |

## Installation

```bash
dotnet add package Fdw.Services.Calculations --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Calculations.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Types.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
