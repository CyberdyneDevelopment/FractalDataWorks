using System;

namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Event raised when a calculation starts.
/// </summary>
public sealed record CalculationStartedEvent(
    string CalculationId,
    string CalculationType,
    string RequestedBy,
    DateTimeOffset StartedAt);