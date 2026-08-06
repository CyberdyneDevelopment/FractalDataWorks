# Fdw.Calculations.Abstractions

Calculation contracts — formulas, dependencies and validation.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (13)

| Type | Kind | Purpose |
|---|---|---|
| `IAggregationBuilder<TOutput>` | interface | Fluent builder interface for configuring aggregation operations. |
| `IBusinessRuleBuilder<TOutput>` | interface | Fluent builder interface for configuring business rule evaluation. |
| `ICalculation<TInput, TOutput>` | interface | Represents a calculation that transforms input data into output data. Calculations follow… |
| `ICalculationBuilder<TOutput>` | interface | Fluent builder interface for constructing calculations. Provides a fluent API for building complex… |
| `ICalculationContext` | interface | Execution context for calculations. |
| `ICalculationType` | interface | Interface for scalar calculation types that reduce rows to a single value. Extends ITypeOption to enable… |
| `ICalculationType<TOut>` | interface | Generic interface for calculation types that produce a typed output. |
| `IDataSourceBuilder<TOutput>` | interface | Fluent builder interface for configuring data sources. |
| `IDataSourceJoinBuilder<TOutput>` | interface | Fluent builder interface for configuring joins between data sources. |
| `IPeriodComparisonBuilder<TOutput>` | interface | Fluent builder interface for configuring period-over-period comparison operations. |
| `IPeriodComparisonType` | interface | Interface for period comparison types used in time-series analysis. Extends ITypeOption to enable… |
| `ITimeSeriesBuilder<TOutput>` | interface | Fluent builder interface for configuring time-series operations. |
| `IWindowedCalculationType` | interface | Interface for windowed calculation types that produce one value per input row. Extends ITypeOption to… |

## Base types (6)

| Type | Kind | Purpose |
|---|---|---|
| `CalculationBase<TInput, TOutput>` | class | Base class for calculation implementations. Provides common functionality and enforces consistent… |
| `CalculationResultCodeBase` | class | Base class for Calculation result codes. |
| `CalculationResultCodes` | class | TypeCollection for Calculation result codes. Codes use the categorized-number scheme: Id == EventId ==… |
| `CalculationTypeBase` | class | Base class for all scalar calculation types. Accepts injected funcs for in-memory execution and SQL… |
| `PeriodComparisonTypeBase` | class | Base class for period comparison types used in time-series analysis. |
| `WindowedCalculationTypeBase` | class | Base class for all windowed calculation types. Accepts injected funcs for in-memory execution and SQL… |

## Models and supporting types (36)

| Type | Kind | Purpose |
|---|---|---|
| `AverageCalculationType` | class | Average calculation type - calculates mean of all values. |
| `CalculationTypes` | class | TypeCollection for calculation types. |
| `ChainIdRequiredCode` | class | Chain ID is required before building. |
| `ChainNameRequiredCode` | class | Chain name is required before building. |
| `CommandRequiredCode` | class | Data command is required for query execution. |
| `ConnectionNameRequiredCode` | class | Connection name is required for data retrieval. |
| `ContainerNameRequiredCode` | class | Container name is required for data retrieval. |
| `CountCalculationType` | class | Count calculation type - returns the count of non-null values in the column. |
| `DayOverDayPeriodComparisonType` | class | Day-over-Day comparison (compare to previous day). |
| `DependencyValidationFailedCode` | class | Dependency validation failed - missing required datasets and/or calculations. |
| `EngineNotSupportedCode` | class | Transformation engine not supported for this provider. |
| `ExecutionFailedCode` | class | Execution failed. |

## Installation

```bash
dotnet add package Fdw.Calculations.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
