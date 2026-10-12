# Fdw.Calculations.Aggregations

Aggregation functions as a TypeCollection.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `AggregationFunctions` | class | TypeCollection for aggregation functions. |
| `AggregationTypes` | class | Collection of all aggregation types. |

## Options (15 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AverageAggregationFunction` | class | Average aggregation function - calculates mean of all values. |
| `AverageAggregationType` | class | Aggregation type that calculates the average (mean) of all values. |
| `CountAggregationFunction` | class | Count aggregation function - returns the count of values. |
| `CountAggregationType` | class | Aggregation type that counts the number of values. |
| `FirstAggregationType` | class | Aggregation type that returns the first value in the sequence. |
| `LastAggregationType` | class | Aggregation type that returns the last value in the sequence. |
| `MaxAggregationFunction` | class | Maximum aggregation function - finds the largest value. |
| `MaxAggregationType` | class | Aggregation type that finds the maximum value. |
| `MedianAggregationType` | class | Aggregation type that calculates the median (middle) value. |
| `MinAggregationFunction` | class | Minimum aggregation function - finds the smallest value. |
| `MinAggregationType` | class | Aggregation type that finds the minimum value. |
| `StandardDeviationAggregationType` | class | Aggregation type that calculates the standard deviation of values. |
| `SumAggregationFunction` | class | Sum aggregation function - sums all values. |
| `SumAggregationType` | class | Aggregation type that calculates the sum of all values. |
| `VarianceAggregationType` | class | Aggregation type that calculates the variance of values. |

## Installation

```bash
dotnet add package Fdw.Calculations.Aggregations --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Calculations` · `Fdw.Calculations.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Transformations.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
