using System;

namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Event raised when a calculation completes successfully.
/// </summary>
public sealed record CalculationCompletedEvent(
    string CalculationId,
    CalculationResultSummary Result,
    TimeSpan Duration,
    bool WasCached);