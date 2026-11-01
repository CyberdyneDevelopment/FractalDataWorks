using System;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Interface representing usage statistics for a secret container.
/// Provides operational metrics and performance information.
/// </summary>
/// <remarks>
/// Usage statistics help with capacity planning, performance monitoring,
/// and cost optimization for secret storage operations.
/// </remarks>
public interface ISecretContainerUsage
{
    /// <summary>
    /// Gets the total storage space used by secrets in this container.
    /// </summary>
    /// <value>The used storage space in bytes.</value>
    long UsedStorageBytes { get; }

    /// <summary>
    /// Gets the number of read operations performed on this container.
    /// </summary>
    /// <value>The read operation count.</value>
    long ReadOperations { get; }

    /// <summary>
    /// Gets the number of write operations performed on this container.
    /// </summary>
    /// <value>The write operation count.</value>
    long WriteOperations { get; }

    /// <summary>
    /// Gets the number of delete operations performed on this container.
    /// </summary>
    /// <value>The delete operation count.</value>
    long DeleteOperations { get; }

    /// <summary>
    /// Gets the average response time for operations on this container.
    /// </summary>
    /// <value>The average response time.</value>
    TimeSpan AverageResponseTime { get; }

    /// <summary>
    /// Gets when these usage statistics were last updated.
    /// </summary>
    /// <value>The last update timestamp.</value>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Gets the time period covered by these statistics.
    /// </summary>
    /// <value>The statistics period.</value>
    /// <remarks>
    /// This indicates the time window over which the statistics were collected,
    /// helping interpret the operational metrics appropriately.
    /// </remarks>
    TimeSpan StatisticsPeriod { get; }
}