using System;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using BackoffStrategiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions.BackoffStrategies;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.TypeCollections.BackoffStrategies;

/// <summary>
/// Backoff strategy with linearly increasing delays between retry attempts.
/// </summary>
/// <remarks>
/// Use this strategy when you want gradually increasing wait times.
/// Delay increases by the initial delay amount each attempt:
/// 1s, 2s, 3s, 4s, etc.
/// </remarks>
[TypeOption(typeof(BackoffStrategiesCollection), "Linear", RestrictToCurrentCompilation = true)]
public sealed class LinearBackoffStrategy : BackoffStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinearBackoffStrategy"/> class.
    /// </summary>
    public LinearBackoffStrategy()
        : base(
            id: 2,
            name: "Linear",
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            multiplier: 1.0,
            jitterFactor: 0.0)
    {
    }

    /// <inheritdoc/>
    public override TimeSpan GetDelay(int attemptNumber)
    {
        // Linear: delay = initialDelay * attemptNumber
        var delay = TimeSpan.FromTicks(InitialDelay.Ticks * attemptNumber);
        return ApplyJitter(ClampToMax(delay));
    }

    /// <inheritdoc/>
    public override string GetPollyBackoffTypeName() => "Linear";
}
