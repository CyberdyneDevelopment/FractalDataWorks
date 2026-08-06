using System;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for notification results.
/// Defines the outcome of sending a notification.
/// </summary>
public interface INotificationResult
{
    /// <summary>
    /// Gets the request ID this result corresponds to.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets whether the notification was sent successfully.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the status of the notification.
    /// </summary>
    INotificationStatus Status { get; }

    /// <summary>
    /// Gets the error message if the notification failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the timestamp when the notification was sent.
    /// </summary>
    DateTimeOffset SentAt { get; }

    /// <summary>
    /// Gets the delivery confirmation ID from the channel provider, if available.
    /// </summary>
    string? DeliveryId { get; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    int RetryCount { get; }
}
