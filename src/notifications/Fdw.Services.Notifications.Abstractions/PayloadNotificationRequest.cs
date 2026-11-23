using System.Collections.Generic;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A notification request with its words and a payload (FDW-6138).</summary>
public sealed class PayloadNotificationRequest : NotificationRequest, IPayloadNotificationRequest
{
    /// <summary>Initializes a new instance of the <see cref="PayloadNotificationRequest"/> class.</summary>
    /// <param name="channelName">The channel.</param>
    /// <param name="recipients">The recipients.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="message">The message.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="correlationId">The correlation id.</param>
    public PayloadNotificationRequest(
        string channelName,
        IReadOnlyList<string> recipients,
        string subject,
        string message,
        INotificationPriority priority,
        NotificationPayload payload,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? correlationId = null)
        : base(channelName, recipients, subject, message, priority, metadata, correlationId)
    {
        Payload = payload;
    }

    /// <inheritdoc />
    public NotificationPayload Payload { get; }
}
