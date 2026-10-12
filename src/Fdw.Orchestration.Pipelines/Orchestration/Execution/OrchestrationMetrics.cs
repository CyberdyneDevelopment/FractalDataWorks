using System.Collections.Generic;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Default implementation of <see cref="IOrchestrationMetrics"/>.
/// </summary>
/// <remarks>
/// Tracks metrics collected during orchestration execution, including step counts,
/// retry statistics, record counts, and cache performance.
/// </remarks>
public sealed class OrchestrationMetrics : IOrchestrationMetrics
{
    private readonly Dictionary<string, object> _customMetrics = [];

    /// <inheritdoc/>
    public int TotalSteps { get; internal set; }

    /// <inheritdoc/>
    public int SucceededSteps { get; internal set; }

    /// <inheritdoc/>
    public int FailedSteps { get; internal set; }

    /// <inheritdoc/>
    public int SkippedSteps { get; internal set; }

    /// <inheritdoc/>
    public int TotalRetryAttempts { get; internal set; }

    /// <inheritdoc/>
    public long TotalRecordsProcessed { get; internal set; }

    /// <inheritdoc/>
    public int CacheHits { get; internal set; }

    /// <inheritdoc/>
    public int CacheMisses { get; internal set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> CustomMetrics => _customMetrics;

    /// <summary>
    /// Gets the cache hit ratio as a percentage (0-100).
    /// </summary>
    public double CacheHitRatio
    {
        get
        {
            var total = CacheHits + CacheMisses;
            return total > 0 ? (double)CacheHits / total * 100 : 0;
        }
    }

    /// <summary>
    /// Gets the success ratio as a percentage (0-100).
    /// </summary>
    public double SuccessRatio
    {
        get
        {
            return TotalSteps > 0 ? (double)SucceededSteps / TotalSteps * 100 : 0;
        }
    }

    /// <summary>
    /// Sets a custom metric value.
    /// </summary>
    /// <param name="key">The metric key.</param>
    /// <param name="value">The metric value.</param>
    public void SetCustomMetric(string key, object value)
    {
        _customMetrics[key] = value;
    }

    /// <summary>
    /// Increments a custom metric value by the specified amount.
    /// </summary>
    /// <param name="key">The metric key.</param>
    /// <param name="amount">The amount to increment by.</param>
    public void IncrementCustomMetric(string key, long amount = 1)
    {
        if (_customMetrics.TryGetValue(key, out var existing) && existing is long currentValue)
        {
            _customMetrics[key] = currentValue + amount;
        }
        else
        {
            _customMetrics[key] = amount;
        }
    }

    /// <summary>
    /// Creates an empty metrics instance.
    /// </summary>
    /// <returns>An empty metrics instance.</returns>
    public static OrchestrationMetrics Empty() => new();

    /// <summary>
    /// Creates metrics from step results.
    /// </summary>
    /// <param name="stepResults">The step results to aggregate.</param>
    /// <returns>Computed metrics.</returns>
    public static OrchestrationMetrics FromStepResults(IEnumerable<IOrchestrationStepResult> stepResults)
    {
        var metrics = new OrchestrationMetrics();

        foreach (var result in stepResults)
        {
            metrics.TotalSteps++;
            metrics.TotalRetryAttempts += result.RetryAttempts;
            metrics.TotalRecordsProcessed += result.RecordsProcessed;

            if (result.WasCached)
            {
                metrics.CacheHits++;
            }
            else
            {
                metrics.CacheMisses++;
            }

            if (result.Status.IsSuccess)
            {
                metrics.SucceededSteps++;
            }
            else if (result.Status.IsFailure)
            {
                metrics.FailedSteps++;
            }
            else if (!result.Status.IsInProgress && !result.Status.IsTerminal)
            {
                metrics.SkippedSteps++;
            }
        }

        return metrics;
    }

    /// <summary>
    /// Merges another metrics instance into this one.
    /// </summary>
    /// <param name="other">The metrics to merge.</param>
    public void Merge(IOrchestrationMetrics other)
    {
        TotalSteps += other.TotalSteps;
        SucceededSteps += other.SucceededSteps;
        FailedSteps += other.FailedSteps;
        SkippedSteps += other.SkippedSteps;
        TotalRetryAttempts += other.TotalRetryAttempts;
        TotalRecordsProcessed += other.TotalRecordsProcessed;
        CacheHits += other.CacheHits;
        CacheMisses += other.CacheMisses;

        foreach (var kvp in other.CustomMetrics)
        {
            if (kvp.Value is long otherValue && _customMetrics.TryGetValue(kvp.Key, out var existing) && existing is long currentValue)
            {
                _customMetrics[kvp.Key] = currentValue + otherValue;
            }
            else
            {
                _customMetrics[kvp.Key] = kvp.Value;
            }
        }
    }
}
