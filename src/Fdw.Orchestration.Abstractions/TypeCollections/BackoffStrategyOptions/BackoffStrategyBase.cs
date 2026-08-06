using System;
using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;

/// <summary>
/// Base class for backoff strategy TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for backoff strategies used in retry policies.
/// Derived classes implement specific delay calculation algorithms.
/// </remarks>
public abstract class BackoffStrategyBase : TypeOptionBase<int, BackoffStrategyBase>, IBackoffStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackoffStrategyBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="initialDelay">Initial delay before first retry.</param>
    /// <param name="maxDelay">Maximum delay between retries.</param>
    /// <param name="multiplier">Multiplier for delay calculation.</param>
    /// <param name="jitterFactor">Jitter factor (0.0 to 1.0).</param>
    protected BackoffStrategyBase(
        int id,
        string name,
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        double multiplier = 2.0,
        double jitterFactor = 0.0)
        : base(id, name)
    {
        InitialDelay = initialDelay;
        MaxDelay = maxDelay;
        Multiplier = multiplier;
        JitterFactor = jitterFactor < 0.0 ? 0.0 : (jitterFactor > 1.0 ? 1.0 : jitterFactor);
    }

    /// <inheritdoc/>
    public TimeSpan InitialDelay { get; }

    /// <inheritdoc/>
    public TimeSpan MaxDelay { get; }

    /// <inheritdoc/>
    public double Multiplier { get; }

    /// <inheritdoc/>
    public double JitterFactor { get; }

    /// <inheritdoc/>
    public bool UsesJitter => JitterFactor > 0;

    /// <inheritdoc/>
    public abstract TimeSpan GetDelay(int attemptNumber);

    /// <inheritdoc/>
    public abstract string GetPollyBackoffTypeName();

    /// <summary>
    /// Applies jitter to a delay value.
    /// </summary>
    /// <param name="delay">The base delay.</param>
    /// <returns>The delay with jitter applied.</returns>
    protected TimeSpan ApplyJitter(TimeSpan delay)
    {
        if (JitterFactor <= 0)
            return delay;

        var jitterRange = delay.TotalMilliseconds * JitterFactor;
        var jitter = (ThreadSafeRandom.NextDouble() * 2 - 1) * jitterRange;
        var jitteredMs = delay.TotalMilliseconds + jitter;
        if (jitteredMs < 0) jitteredMs = 0;
        return TimeSpan.FromMilliseconds(jitteredMs);
    }

    /// <summary>
    /// Thread-safe random number generator for netstandard2.0 compatibility.
    /// </summary>
    /// <remarks>
    /// Uses standard Random for jitter timing - not security-sensitive.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Used for timing jitter, not security")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1840:Use Environment.CurrentManagedThreadId", Justification = "netstandard2.0 compatibility")]
    private static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random? _local;

        public static double NextDouble()
        {
            _local ??= new Random(unchecked(Environment.TickCount * 31 + System.Threading.Thread.CurrentThread.ManagedThreadId));
            return _local.NextDouble();
        }
    }

    /// <summary>
    /// Clamps a delay to the maximum delay.
    /// </summary>
    /// <param name="delay">The calculated delay.</param>
    /// <returns>The delay clamped to MaxDelay.</returns>
    protected TimeSpan ClampToMax(TimeSpan delay)
    {
        return delay > MaxDelay ? MaxDelay : delay;
    }
}
