using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Fdw.Calculations.Contracts.Hubs;
using Fdw.SignalR;

namespace Fdw.Calculations.SignalR;

/// <summary>
/// Default implementation of <see cref="ICalculationNotifier"/> using SignalR.
/// </summary>
public sealed class CalculationNotifier
    : SignalRBroadcaster<CalculationHub, ICalculationHubClient>, ICalculationNotifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNotifier"/> class.
    /// </summary>
    public CalculationNotifier(
        IHubContext<CalculationHub, ICalculationHubClient> hubContext,
        ILogger<CalculationNotifier> logger)
        : base(hubContext, logger)
    {
    }

    /// <inheritdoc/>
    public Task NotifyStarted(
        string calculationId,
        string calculationType,
        string requestedBy,
        CancellationToken ct)
    {
        var evt = new CalculationStartedEvent(
            calculationId,
            calculationType,
            requestedBy,
            DateTimeOffset.UtcNow);

        return BroadcastToGroups(
            evt,
            (client, e) => client.CalculationStarted(e),
            $"calc:{calculationId}",
            $"user:{requestedBy}",
            "all-calculations");
    }

    /// <inheritdoc/>
    public Task NotifyProgress(
        string calculationId,
        int percentComplete,
        string currentStep,
        long rowsProcessed,
        long totalRows,
        CancellationToken ct)
    {
        var evt = new CalculationProgressEvent(
            calculationId,
            percentComplete,
            currentStep,
            rowsProcessed,
            totalRows);

        // Only send to calculation-specific subscribers to reduce noise
        return BroadcastToGroup(
            evt,
            (client, e) => client.CalculationProgress(e),
            $"calc:{calculationId}");
    }

    /// <inheritdoc/>
    public Task NotifyCompleted(
        string calculationId,
        CalculationResultSummary result,
        TimeSpan duration,
        bool wasCached,
        CancellationToken ct)
    {
        var evt = new CalculationCompletedEvent(
            calculationId,
            result,
            duration,
            wasCached);

        return BroadcastToGroups(
            evt,
            (client, e) => client.CalculationCompleted(e),
            $"calc:{calculationId}",
            "all-calculations");
    }

    /// <inheritdoc/>
    public Task NotifyFailed(
        string calculationId,
        string errorCode,
        string errorMessage,
        bool isRetryable,
        CancellationToken ct)
    {
        var evt = new CalculationFailedEvent(
            calculationId,
            errorCode,
            errorMessage,
            isRetryable);

        return BroadcastToGroups(
            evt,
            (client, e) => client.CalculationFailed(e),
            $"calc:{calculationId}",
            "all-calculations");
    }

    /// <inheritdoc/>
    public Task BroadcastCacheStatistics(
        CacheStatisticsEvent stats,
        CancellationToken ct)
    {
        return BroadcastToGroup(
            stats,
            (client, e) => client.CacheStatisticsUpdated(e),
            "all-calculations");
    }
}
