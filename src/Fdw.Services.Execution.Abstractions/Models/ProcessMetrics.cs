using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Execution.Abstractions.Models;

/// <summary>
/// Default implementation of process metrics.
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessMetrics : IProcessMetrics
{
    /// <summary>
    /// CPU time consumed during execution.
    /// </summary>
    public TimeSpan CpuTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Peak memory usage during execution.
    /// </summary>
    public long PeakMemoryBytes { get; init; }

    /// <summary>
    /// Number of items processed (if applicable).
    /// </summary>
    public long ItemsProcessed { get; init; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Custom performance counters.
    /// </summary>
    public IReadOnlyDictionary<string, long> Counters { get; init; } = new Dictionary<string, long>(StringComparer.Ordinal);

    /// <summary>
    /// Custom timing measurements.
    /// </summary>
    public IReadOnlyDictionary<string, TimeSpan> Timings { get; init; } = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

    /// <summary>
    /// Get empty metrics.
    /// </summary>
    public static ProcessMetrics Empty => new();

    /// <summary>
    /// Get metrics with item count.
    /// </summary>
    /// <param name="itemsProcessed">Number of items processed.</param>
    /// <returns>ProcessMetrics instance with the specified item count.</returns>
    public static ProcessMetrics WithItemCount(long itemsProcessed) => new() { ItemsProcessed = itemsProcessed };

    /// <summary>
    /// Get metrics with custom counters.
    /// </summary>
    /// <param name="counters">Custom performance counters.</param>
    /// <returns>ProcessMetrics instance with the specified counters.</returns>
    public static ProcessMetrics WithCounters(IDictionary<string, long> counters) => new() { Counters = new Dictionary<string, long>(counters, StringComparer.Ordinal) };
}