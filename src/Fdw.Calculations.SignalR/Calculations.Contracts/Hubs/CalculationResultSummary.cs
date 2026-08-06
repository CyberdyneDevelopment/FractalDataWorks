namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Summary of a calculation result.
/// </summary>
public sealed record CalculationResultSummary(
    decimal Result,
    int InputCount,
    long ExecutionTimeMs);