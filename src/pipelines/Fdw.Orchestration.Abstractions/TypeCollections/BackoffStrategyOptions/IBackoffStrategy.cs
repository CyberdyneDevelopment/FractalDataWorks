using System;
using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;

/// <summary>
/// Interface for backoff strategy TypeOptions.
/// </summary>
/// <remarks>
/// Backoff strategies define how delays between retry attempts are calculated.
/// Strategies can map to Polly's DelayBackoffType for resilience pipeline integration.
/// </remarks>
public interface IBackoffStrategy : ITypeOption<int, BackoffStrategyBase>
{
    /// <summary>
    /// Gets the initial delay before the first retry.
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the multiplier for exponential/linear strategies.
    /// </summary>
    double Multiplier { get; }

    /// <summary>
    /// Gets the jitter factor (0.0 to 1.0) for adding randomness to delays.
    /// </summary>
    double JitterFactor { get; }

    /// <summary>
    /// Gets whether this strategy uses jitter (randomized delays).
    /// </summary>
    bool UsesJitter { get; }

    /// <summary>
    /// Calculates the delay for a specific retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The attempt number (1-based).</param>
    /// <returns>The delay before the next retry.</returns>
    TimeSpan GetDelay(int attemptNumber);

    /// <summary>
    /// Gets the Polly DelayBackoffType that corresponds to this strategy.
    /// </summary>
    /// <returns>The Polly backoff type name (Constant, Linear, Exponential).</returns>
    string GetPollyBackoffTypeName();
}
