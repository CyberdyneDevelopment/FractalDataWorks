using System;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Concrete implementation of a notification result.
/// </summary>
public sealed class NotificationResult : INotificationResult
{
    private NotificationResult(
        string requestId,
        bool isSuccess,
        INotificationStatus status,
        string? errorMessage,
        DateTimeOffset sentAt,
        string? deliveryId,
        int retryCount)
    {
        RequestId = requestId;
        IsSuccess = isSuccess;
        Status = status;
        ErrorMessage = errorMessage;
        SentAt = sentAt;
        DeliveryId = deliveryId;
        RetryCount = retryCount;
    }

    /// <inheritdoc/>
    public string RequestId { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public INotificationStatus Status { get; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; }

    /// <inheritdoc/>
    public DateTimeOffset SentAt { get; }

    /// <inheritdoc/>
    public string? DeliveryId { get; }

    /// <inheritdoc/>
    public int RetryCount { get; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="deliveryId">Optional delivery ID from the channel.</param>
    /// <returns>A success result.</returns>
    public static NotificationResult Success(string requestId, string? deliveryId = null)
    {
        return new NotificationResult(
            requestId,
            isSuccess: true,
            status: NotificationStatuses.Sent,
            errorMessage: null,
            sentAt: DateTimeOffset.UtcNow,
            deliveryId: deliveryId,
            retryCount: 0);
    }

    /// <summary>
    /// Creates a success result with delivery confirmation.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="deliveryId">The delivery ID from the channel.</param>
    /// <returns>A delivered result.</returns>
    public static NotificationResult Delivered(string requestId, string deliveryId)
    {
        return new NotificationResult(
            requestId,
            isSuccess: true,
            status: NotificationStatuses.Delivered,
            errorMessage: null,
            sentAt: DateTimeOffset.UtcNow,
            deliveryId: deliveryId,
            retryCount: 0);
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="retryCount">The number of retries attempted.</param>
    /// <returns>A failure result.</returns>
    public static NotificationResult Failed(string requestId, string errorMessage, int retryCount = 0)
    {
        return new NotificationResult(
            requestId,
            isSuccess: false,
            status: NotificationStatuses.Failed,
            errorMessage: errorMessage,
            sentAt: DateTimeOffset.UtcNow,
            deliveryId: null,
            retryCount: retryCount);
    }

    /// <summary>
    /// Creates a rejected result.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="reason">The rejection reason.</param>
    /// <returns>A rejected result.</returns>
    public static NotificationResult Rejected(string requestId, string reason)
    {
        return new NotificationResult(
            requestId,
            isSuccess: false,
            status: NotificationStatuses.Rejected,
            errorMessage: reason,
            sentAt: DateTimeOffset.UtcNow,
            deliveryId: null,
            retryCount: 0);
    }

    /// <summary>
    /// Creates a pending result.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <returns>A pending result.</returns>
    public static NotificationResult Pending(string requestId)
    {
        return new NotificationResult(
            requestId,
            isSuccess: false,
            status: NotificationStatuses.Pending,
            errorMessage: null,
            sentAt: DateTimeOffset.UtcNow,
            deliveryId: null,
            retryCount: 0);
    }
}
