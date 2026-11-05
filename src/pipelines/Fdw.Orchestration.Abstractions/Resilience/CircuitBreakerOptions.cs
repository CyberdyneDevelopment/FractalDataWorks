using System;

namespace Fdw.Orchestration.Abstractions.Resilience;

/// <summary>
/// A circuit breaker's values. Whoever turns one on states every one of them; nothing assumes a ratio, a throughput or a duration
/// (FDW-3931: "you are defining a resilience policy. why in the fuck would you have a default?").
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Gets the share of failed calls, from 0 to 1, that opens the circuit.</summary>
    public required double FailureRatio { get; init; }

    /// <summary>Gets the fewest calls in a sampling window before the ratio is judged.</summary>
    public required int MinimumThroughput { get; init; }

    /// <summary>Gets how long the circuit stays open before it lets a call through to test.</summary>
    public required TimeSpan BreakDuration { get; init; }

    /// <summary>Gets the window over which the failure ratio is measured. The factory held 30 seconds in code before FDW-3931.</summary>
    public required TimeSpan SamplingDuration { get; init; }
}
