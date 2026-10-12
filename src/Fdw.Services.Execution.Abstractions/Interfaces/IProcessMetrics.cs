using System;
using System.Collections.Generic;

namespace Fdw.Services.Execution.Abstractions;

/// <summary>
/// Performance metrics for process execution.
/// </summary>
public interface IProcessMetrics
{
    /// <summary>
    /// CPU time consumed during execution.
    /// </summary>
    TimeSpan CpuTime { get; }

    /// <summary>
    /// Peak memory usage during execution.
    /// </summary>
    long PeakMemoryBytes { get; }

    /// <summary>
    /// Number of items processed (if applicable).
    /// </summary>
    long ItemsProcessed { get; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    int RetryAttempts { get; }

    /// <summary>
    /// Custom performance counters.
    /// </summary>
    IReadOnlyDictionary<string, long> Counters { get; }

    /// <summary>
    /// Custom timing measurements.
    /// </summary>
    IReadOnlyDictionary<string, TimeSpan> Timings { get; }
}