using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Result of executing a quality check.
/// </summary>
public sealed record QualityCheckResult(
    Guid RuleId,
    string RuleName,
    string RuleType,
    bool Passed,
    int TotalRecords,
    int PassedRecords,
    int FailedRecords,
    double PassRate,
    DateTimeOffset ExecutedAt,
    IReadOnlyList<QualityViolation> SampleViolations);