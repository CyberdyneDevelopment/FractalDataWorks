using System;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using BackoffStrategiesCollection = Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions.BackoffStrategies;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.TypeCollections.BackoffStrategies;

/// <summary>
/// Backoff strategy with exponentially increasing delays between retry attempts.
/// </summary>
/// <remarks>
/// Use this strategy for transient failures where rapid initial retries followed
/// by longer waits is beneficial. Delay doubles each attempt:
/// 1s, 2s, 4s, 8s, 16s, etc.
/// </remarks>
[TypeOption(typeof(BackoffStrategiesCollection), "Exponential", RestrictToCurrentCompilation = true)]
public sealed class ExponentialBackoffStrategy : BackoffStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffStrategy"/> class.
    /// </summary>
    public ExponentialBackoffStrategy()
        : base(
            id: 3,
            name: "Exponential",
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(2),
            multiplier: 2.0,
            jitterFactor: 0.0)
    {
    }

    /// <inheritdoc/>
    public override TimeSpan GetDelay(int attemptNumber)
    {
        // Exponential: delay = initialDelay * (multiplier ^ (attemptNumber - 1))
        var multiplier = Math.Pow(Multiplier, attemptNumber - 1);
        var delayMs = InitialDelay.TotalMilliseconds * multiplier;
        var delay = TimeSpan.FromMilliseconds(delayMs);
        return ApplyJitter(ClampToMax(delay));
    }

    /// <inheritdoc/>
    public override string GetPollyBackoffTypeName() => "Exponential";
}
