namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Event raised to report calculation progress.
/// </summary>
public sealed record CalculationProgressEvent(
    string CalculationId,
    int PercentComplete,
    string CurrentStep,
    long RowsProcessed,
    long TotalRows);