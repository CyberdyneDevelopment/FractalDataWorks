# Fdw.Services.Quality.Abstractions

The data-quality contracts — rules, severities and evaluation outcomes.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (3)

| Type | Kind | Purpose |
|---|---|---|
| `IPromotionStatusType` | interface | Represents a status for promotion workflow tracking. |
| `IQualityRuleType` | interface | Represents a quality rule type that defines validation logic for data quality checks. |
| `IQualitySeverityType` | interface | Represents a severity level for quality rule violations. |

## Base types (3)

| Type | Kind | Purpose |
|---|---|---|
| `PromotionStatusTypeBase` | class | Base class for promotion status types using the CRTP pattern. |
| `QualityRuleTypeBase` | class | Base class for quality rule types using the CRTP pattern. |
| `QualitySeverityTypeBase` | class | Base class for quality severity types using the CRTP pattern. |

## Models and supporting types (27)

| Type | Kind | Purpose |
|---|---|---|
| `ApprovedStatusType` | class | Approved status type indicating a promotion is approved and ready to execute. |
| `CompletedStatusType` | class | Completed status type indicating a promotion has successfully completed. |
| `CreateQualityRulePayload` | class | Create quality rule request. |
| `CustomExpressionRuleType` | class | Rule type that validates a field using a custom C# expression. |
| `DistinctCountInRangeRuleType` | class | Rule type that validates the distinct count (cardinality) of a field is within specified bounds. |
| `ErrorSeverityType` | class | Error severity type indicating critical quality violations that block processing. |
| `FailedStatusType` | class | Failed status type indicating a promotion execution has failed. |
| `InProgressStatusType` | class | InProgress status type indicating a promotion is currently executing. |
| `InRangeRuleType` | class | Rule type that validates a field value is within specified minimum and maximum bounds. |
| `InReferenceSetRuleType` | class | Rule type that validates a field value exists in an allowed set of values. |
| `InfoSeverityType` | class | Info severity type indicating informational quality observations that don't require action. |
| `MatchesPatternRuleType` | class | Rule type that validates a field value matches a regular expression pattern. |

## Installation

```bash
dotnet add package Fdw.Services.Quality.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
