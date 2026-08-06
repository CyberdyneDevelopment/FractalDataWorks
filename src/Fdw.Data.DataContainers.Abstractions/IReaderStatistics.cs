using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Provides statistics about a data reading operation.
/// </summary>
/// <remarks>
/// IReaderStatistics offers insights into reading performance and progress,
/// which can be valuable for monitoring, optimization, and user feedback.
/// </remarks>
public interface IReaderStatistics
{
    /// <summary>
    /// Gets the total number of records read so far.
    /// </summary>
    /// <value>The number of records that have been successfully read.</value>
    long RecordsRead { get; }

    /// <summary>
    /// Gets the total number of bytes read from the source.
    /// </summary>
    /// <value>The number of bytes read, or -1 if not measurable.</value>
    long BytesRead { get; }

    /// <summary>
    /// Gets the elapsed time since reading began.
    /// </summary>
    /// <value>The total elapsed time for the reading operation.</value>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the average records per second reading rate.
    /// </summary>
    /// <value>The reading rate in records per second, or 0 if not measurable.</value>
    double RecordsPerSecond { get; }

    /// <summary>
    /// Gets the number of errors encountered during reading.
    /// </summary>
    /// <value>The count of non-fatal errors that were handled during reading.</value>
    int ErrorCount { get; }

    /// <summary>
    /// Gets additional statistics specific to the container type.
    /// </summary>
    /// <value>Container-specific metrics and performance data.</value>
    IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
}