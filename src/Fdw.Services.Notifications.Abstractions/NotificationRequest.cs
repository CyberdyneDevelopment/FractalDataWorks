using System;
using System.Collections.Generic;
using Fdw.Abstractions;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Concrete implementation of a notification request.
/// </summary>
public sealed class NotificationRequest : INotificationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRequest"/> class.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <param name="recipients">The recipients.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="message">The message.</param>
    /// <param name="priority">The priority.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public NotificationRequest(
        string channelName,
        IReadOnlyList<string> recipients,
        string subject,
        string message,
        INotificationPriority? priority = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string? correlationId = null)
    {
        CommandId = Guid.NewGuid();
        RequestId = CommandId.ToString();
        ChannelName = channelName;
        Recipients = recipients;
        Subject = subject;
        Message = message;
        Priority = priority ?? NotificationPriorities.Normal;
        Metadata = metadata;
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public Guid CommandId { get; }

    /// <inheritdoc/>
    DateTime IGenericCommand.CreatedAt => CreatedAt.DateTime;

    /// <inheritdoc/>
    public string CommandType => "Notification";

    /// <inheritdoc/>
    public string Category => "Notifications";

    /// <inheritdoc/>
    public string RequestId { get; }

    /// <inheritdoc/>
    public string ChannelName { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> Recipients { get; }

    /// <inheritdoc/>
    public string Subject { get; }

    /// <inheritdoc/>
    public string Message { get; }

    /// <inheritdoc/>
    public INotificationPriority Priority { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <inheritdoc/>
    public string? CorrelationId { get; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Creates a new notification request builder.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <returns>A new builder instance.</returns>
    public static NotificationRequestBuilder Create(string channelName) => new(channelName);
}
