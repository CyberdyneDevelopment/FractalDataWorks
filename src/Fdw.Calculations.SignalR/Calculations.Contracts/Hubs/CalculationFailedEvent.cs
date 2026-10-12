namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Event raised when a calculation fails.
/// </summary>
public sealed record CalculationFailedEvent(
    string CalculationId,
    string ErrorCode,
    string ErrorMessage,
    bool IsRetryable);