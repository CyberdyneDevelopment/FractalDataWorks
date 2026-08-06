using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Provides statistics about a data writing operation.
/// </summary>
/// <remarks>
/// IWriterStatistics offers insights into writing performance and progress,
/// which can be valuable for monitoring, optimization, and user feedback.
/// </remarks>
public interface IWriterStatistics
{
    /// <summary>
    /// Gets the total number of records written so far.
    /// </summary>
    /// <value>The number of records that have been successfully written.</value>
    long RecordsWritten { get; }

    /// <summary>
    /// Gets the total number of bytes written to the destination.
    /// </summary>
    /// <value>The number of bytes written, or -1 if not measurable.</value>
    long BytesWritten { get; }

    /// <summary>
    /// Gets the elapsed time since writing began.
    /// </summary>
    /// <value>The total elapsed time for the writing operation.</value>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the average records per second writing rate.
    /// </summary>
    /// <value>The writing rate in records per second, or 0 if not measurable.</value>
    double RecordsPerSecond { get; }

    /// <summary>
    /// Gets the number of errors encountered during writing.
    /// </summary>
    /// <value>The count of non-fatal errors that were handled during writing.</value>
    int ErrorCount { get; }

    /// <summary>
    /// Gets the number of validation errors encountered.
    /// </summary>
    /// <value>The count of records that failed schema validation.</value>
    int ValidationErrors { get; }

    /// <summary>
    /// Gets additional statistics specific to the container type.
    /// </summary>
    /// <value>Container-specific metrics and performance data.</value>
    IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
}