using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Service for sending real-time schema discovery notifications via SignalR.
/// </summary>
public interface ISchemaDiscoveryNotifier
{
    /// <summary>
    /// Notifies subscribers that schema discovery has started.
    /// </summary>
    /// <param name="discoveryId">The discovery ID.</param>
    /// <param name="connectionName">The connection being discovered.</param>
    /// <param name="schemaFilter">Optional schema filter applied.</param>
    /// <param name="requestedBy">Who requested the discovery.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyStarted(
        string discoveryId,
        string connectionName,
        string? schemaFilter,
        string requestedBy,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers of discovery progress.
    /// </summary>
    /// <param name="discoveryId">The discovery ID.</param>
    /// <param name="percentComplete">Progress percentage (0-100).</param>
    /// <param name="currentStep">Current step description.</param>
    /// <param name="objectsDiscovered">Objects discovered so far.</param>
    /// <param name="estimatedTotal">Estimated total objects, if known.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyProgress(
        string discoveryId,
        int percentComplete,
        string currentStep,
        int objectsDiscovered,
        int? estimatedTotal,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers that a schema object was discovered.
    /// </summary>
    /// <param name="discoveryId">The discovery ID.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The object name.</param>
    /// <param name="objectType">The object type (Table/View).</param>
    /// <param name="columnCount">Number of columns.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyObjectDiscovered(
        string discoveryId,
        string schemaName,
        string objectName,
        string objectType,
        int columnCount,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers that schema discovery completed successfully.
    /// </summary>
    /// <param name="discoveryId">The discovery ID.</param>
    /// <param name="summary">The discovery result summary.</param>
    /// <param name="duration">Time taken for the discovery.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyCompleted(
        string discoveryId,
        SchemaDiscoverySummary summary,
        TimeSpan duration,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers that schema discovery failed.
    /// </summary>
    /// <param name="discoveryId">The discovery ID.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="partialObjectsDiscovered">Number of objects discovered before failure.</param>
    /// <param name="isRetryable">Whether the operation can be retried.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyFailed(
        string discoveryId,
        string errorCode,
        string errorMessage,
        int partialObjectsDiscovered,
        bool isRetryable,
        CancellationToken ct);
}
