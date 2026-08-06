using System;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using BackoffStrategiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions.BackoffStrategies;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.TypeCollections.BackoffStrategies;

/// <summary>
/// Backoff strategy with decorrelated jitter for distributed systems.
/// </summary>
/// <remarks>
/// Use this strategy in distributed systems to prevent thundering herd problems.
/// The jitter introduces randomness that decorrelates retry attempts across
/// multiple instances, reducing the chance of simultaneous retries.
/// Based on the AWS architecture blog recommendations for exponential backoff.
/// </remarks>
[TypeOption(typeof(BackoffStrategiesCollection), "DecorrelatedJitter", RestrictToCurrentCompilation = true)]
public sealed class DecorrelatedJitterBackoffStrategy : BackoffStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecorrelatedJitterBackoffStrategy"/> class.
    /// </summary>
    public DecorrelatedJitterBackoffStrategy()
        : base(
            id: 4,
            name: "DecorrelatedJitter",
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(2),
            multiplier: 2.0,
            jitterFactor: 0.5)
    {
    }

    /// <inheritdoc/>
    public override TimeSpan GetDelay(int attemptNumber)
    {
        // Decorrelated jitter: delay = random between initialDelay and previousDelay * 3
        // For first attempt, use exponential base
        var baseDelayMs = InitialDelay.TotalMilliseconds * Math.Pow(Multiplier, attemptNumber - 1);
        var delay = TimeSpan.FromMilliseconds(baseDelayMs);

        // Apply significant jitter for decorrelation
        var jitteredDelay = ApplyJitter(delay);
        return ClampToMax(jitteredDelay);
    }

    /// <inheritdoc/>
    public override string GetPollyBackoffTypeName() => "Exponential";
}
