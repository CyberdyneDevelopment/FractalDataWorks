using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Data.DataStores.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents metrics about a data container's characteristics.
/// </summary>
/// <remarks>
/// ContainerMetrics provides information for query optimization, resource
/// planning, and performance tuning. Different container types may provide
/// different levels of metric detail.
/// </remarks>
public sealed class ContainerMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerMetrics"/> class.
    /// </summary>
    /// <param name="estimatedRecordCount">The estimated number of records.</param>
    /// <param name="estimatedDataSize">The estimated data size in bytes.</param>
    /// <param name="lastModified">When the data was last modified.</param>
    /// <param name="additionalMetrics">Additional container-specific metrics.</param>
    public ContainerMetrics(
        long estimatedRecordCount,
        long estimatedDataSize,
        DateTimeOffset? lastModified = null,
        IDictionary<string, object>? additionalMetrics = null)
    {
        EstimatedRecordCount = estimatedRecordCount;
        EstimatedDataSize = estimatedDataSize;
        LastModified = lastModified;
        AdditionalMetrics = additionalMetrics != null
            ? new Dictionary<string, object>(additionalMetrics, StringComparer.Ordinal)
            : new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the estimated number of records in the container.
    /// </summary>
    /// <value>The estimated record count, or -1 if unknown.</value>
    public long EstimatedRecordCount { get; }

    /// <summary>
    /// Gets the estimated data size in bytes.
    /// </summary>
    /// <value>The estimated size, or -1 if unknown.</value>
    public long EstimatedDataSize { get; }

    /// <summary>
    /// Gets when the data was last modified.
    /// </summary>
    /// <value>The last modified timestamp, or null if unknown.</value>
    public DateTimeOffset? LastModified { get; }

    /// <summary>
    /// Gets additional container-specific metrics.
    /// </summary>
    /// <value>
    /// Additional metrics that may be specific to the container type,
    /// such as compression ratio, index information, or format-specific data.
    /// </value>
    public IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
}