using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Contracts.Hubs;

namespace Fdw.Calculations.SignalR;

/// <summary>
/// Service for sending real-time calculation notifications via SignalR.
/// </summary>
public interface ICalculationNotifier
{
    /// <summary>
    /// Notifies subscribers that a calculation has started.
    /// </summary>
    /// <param name="calculationId">The calculation ID.</param>
    /// <param name="calculationType">The type of calculation.</param>
    /// <param name="requestedBy">Who requested the calculation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyStarted(
        string calculationId,
        string calculationType,
        string requestedBy,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers of calculation progress.
    /// </summary>
    /// <param name="calculationId">The calculation ID.</param>
    /// <param name="percentComplete">Progress percentage (0-100).</param>
    /// <param name="currentStep">Current step description.</param>
    /// <param name="rowsProcessed">Rows processed so far.</param>
    /// <param name="totalRows">Total rows to process.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyProgress(
        string calculationId,
        int percentComplete,
        string currentStep,
        long rowsProcessed,
        long totalRows,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers that a calculation completed successfully.
    /// </summary>
    /// <param name="calculationId">The calculation ID.</param>
    /// <param name="result">The calculation result summary.</param>
    /// <param name="duration">Time taken for the calculation.</param>
    /// <param name="wasCached">Whether the result was served from cache.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyCompleted(
        string calculationId,
        CalculationResultSummary result,
        TimeSpan duration,
        bool wasCached,
        CancellationToken ct);

    /// <summary>
    /// Notifies subscribers that a calculation failed.
    /// </summary>
    /// <param name="calculationId">The calculation ID.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="isRetryable">Whether the operation can be retried.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyFailed(
        string calculationId,
        string errorCode,
        string errorMessage,
        bool isRetryable,
        CancellationToken ct);

    /// <summary>
    /// Broadcasts cache statistics to all subscribers.
    /// </summary>
    /// <param name="stats">The cache statistics event.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastCacheStatistics(
        CacheStatisticsEvent stats,
        CancellationToken ct);
}
