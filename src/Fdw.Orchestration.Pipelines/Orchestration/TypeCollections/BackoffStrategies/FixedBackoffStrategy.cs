using System;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using BackoffStrategiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions.BackoffStrategies;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.TypeCollections.BackoffStrategies;

/// <summary>
/// Backoff strategy with a constant delay between retry attempts.
/// </summary>
/// <remarks>
/// Use this strategy when you want consistent wait times between retries.
/// The delay is always the same regardless of attempt number.
/// </remarks>
[TypeOption(typeof(BackoffStrategiesCollection), "Fixed", RestrictToCurrentCompilation = true)]
public sealed class FixedBackoffStrategy : BackoffStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedBackoffStrategy"/> class.
    /// </summary>
    public FixedBackoffStrategy()
        : base(
            id: 1,
            name: "Fixed",
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(1),
            multiplier: 1.0,
            jitterFactor: 0.0)
    {
    }

    /// <inheritdoc/>
    public override TimeSpan GetDelay(int attemptNumber)
    {
        // Always return the same delay
        return ApplyJitter(InitialDelay);
    }

    /// <inheritdoc/>
    public override string GetPollyBackoffTypeName() => "Constant";
}
