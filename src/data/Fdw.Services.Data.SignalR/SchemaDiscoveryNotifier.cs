using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Fdw.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Default implementation of <see cref="ISchemaDiscoveryNotifier"/> using SignalR.
/// </summary>
public sealed class SchemaDiscoveryNotifier
    : SignalRBroadcaster<SchemaDiscoveryHub, ISchemaDiscoveryHubClient>, ISchemaDiscoveryNotifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryNotifier"/> class.
    /// </summary>
    public SchemaDiscoveryNotifier(
        IHubContext<SchemaDiscoveryHub, ISchemaDiscoveryHubClient> hubContext,
        ILogger<SchemaDiscoveryNotifier> logger)
        : base(hubContext, logger)
    {
    }

    /// <inheritdoc/>
    public Task NotifyStarted(
        string discoveryId,
        string connectionName,
        string? schemaFilter,
        string requestedBy,
        CancellationToken ct)
    {
        var evt = new SchemaDiscoveryStartedEvent(
            discoveryId,
            connectionName,
            schemaFilter,
            DateTimeOffset.UtcNow,
            requestedBy);

        return BroadcastToGroups(
            evt,
            (client, e) => client.DiscoveryStarted(e),
            $"discovery:{discoveryId}",
            $"user:{requestedBy}",
            "all-discoveries");
    }

    /// <inheritdoc/>
    public Task NotifyProgress(
        string discoveryId,
        int percentComplete,
        string currentStep,
        int objectsDiscovered,
        int? estimatedTotal,
        CancellationToken ct)
    {
        var evt = new SchemaDiscoveryProgressEvent(
            discoveryId,
            percentComplete,
            currentStep,
            objectsDiscovered,
            estimatedTotal);

        // Only send to discovery-specific subscribers to reduce noise
        return BroadcastToGroup(
            evt,
            (client, e) => client.DiscoveryProgress(e),
            $"discovery:{discoveryId}");
    }

    /// <inheritdoc/>
    public Task NotifyObjectDiscovered(
        string discoveryId,
        string schemaName,
        string objectName,
        string objectType,
        int columnCount,
        CancellationToken ct)
    {
        var evt = new SchemaObjectDiscoveredEvent(
            discoveryId,
            schemaName,
            objectName,
            objectType,
            columnCount);

        // Only send to discovery-specific subscribers
        return BroadcastToGroup(
            evt,
            (client, e) => client.ObjectDiscovered(e),
            $"discovery:{discoveryId}");
    }

    /// <inheritdoc/>
    public Task NotifyCompleted(
        string discoveryId,
        SchemaDiscoverySummary summary,
        TimeSpan duration,
        CancellationToken ct)
    {
        var evt = new SchemaDiscoveryCompletedEvent(
            discoveryId,
            summary,
            duration);

        return BroadcastToGroups(
            evt,
            (client, e) => client.DiscoveryCompleted(e),
            $"discovery:{discoveryId}",
            "all-discoveries");
    }

    /// <inheritdoc/>
    public Task NotifyFailed(
        string discoveryId,
        string errorCode,
        string errorMessage,
        int partialObjectsDiscovered,
        bool isRetryable,
        CancellationToken ct)
    {
        var evt = new SchemaDiscoveryFailedEvent(
            discoveryId,
            errorCode,
            errorMessage,
            partialObjectsDiscovered,
            isRetryable);

        return BroadcastToGroups(
            evt,
            (client, e) => client.DiscoveryFailed(e),
            $"discovery:{discoveryId}",
            "all-discoveries");
    }
}
