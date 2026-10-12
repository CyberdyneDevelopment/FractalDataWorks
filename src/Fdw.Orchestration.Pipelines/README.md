# Fdw.Orchestration.Pipelines

Pipeline orchestration: composing steps into a runnable pipeline.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PipelineResultCodes` | class | TypeCollection for Pipeline result codes. EventId range: 5700-5799 (within Orchestration domain) |

## Options (46 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AllowedValuesRequiredCode` | class | InList validation requires 'AllowedValues' parameter. |
| `AppendWriteMode` | class | Write mode that appends new data to existing data. |
| `CancelledStatus` | class | Execution status indicating the orchestration was cancelled by user or system. |
| `CompensateMode` | class | Error handling mode that triggers compensation logic (saga pattern). |
| `CreateNewWriteMode` | class | Write mode that only creates new records, failing if they exist. |
| `CriticalSeverity` | class | Critical validation severity that always blocks execution. |
| `CustomStageType` | class | Stage type for custom processing logic. |
| `CustomValidationRuleType` | class | Validation rule for custom validation logic. |
| `DataTypeParameterRequiredCode` | class | DataType validation requires a 'DataType' parameter. |
| `DataTypeValidationRuleType` | class | Validation rule that checks if values can be converted to a specified data type. |
| `DecorrelatedJitterBackoffStrategy` | class | Backoff strategy with decorrelated jitter for distributed systems. |
| `ErrorSeverity` | class | Error validation severity that may block execution depending on configuration. |
| `ExponentialBackoffStrategy` | class | Backoff strategy with exponentially increasing delays between retry attempts. |
| `ExtractStageRequiresConfigurationCode` | class | Extract stage requires configuration. |
| `ExtractStageType` | class | Stage type for extracting data from a source. |
| `FailedStatus` | class | Execution status indicating the orchestration failed. |
| `FixedBackoffStrategy` | class | Backoff strategy with a constant delay between retry attempts. |
| `InListValidationRuleType` | class | Validation rule that checks if values are in a predefined list of allowed values. |

## Installation

```bash
dotnet add package Fdw.Orchestration.Pipelines --prerelease
```

## Dependencies

`Fdw.Orchestration.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
