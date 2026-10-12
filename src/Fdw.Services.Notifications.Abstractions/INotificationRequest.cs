using System;
using System.Collections.Generic;
using Fdw.Abstractions;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for notification requests.
/// Extends IGenericCommand so notifications can flow through the command pipeline.
/// </summary>
public interface INotificationRequest : IGenericCommand
{
    /// <summary>
    /// Gets the unique identifier for this notification request.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the name of the channel to send the notification through.
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Gets the list of recipients for this notification.
    /// For email, these are email addresses. For Teams, these are webhook URLs.
    /// </summary>
    IReadOnlyList<string> Recipients { get; }

    /// <summary>
    /// Gets the subject or title of the notification.
    /// </summary>
    string Subject { get; }

    /// <summary>
    /// Gets the message body of the notification.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the priority of this notification.
    /// </summary>
    INotificationPriority Priority { get; }

    /// <summary>
    /// Gets optional metadata associated with this notification.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <summary>
    /// Gets the correlation ID for tracing this notification across systems.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the timestamp when this request was created.
    /// </summary>
    new DateTimeOffset CreatedAt { get; }
}
