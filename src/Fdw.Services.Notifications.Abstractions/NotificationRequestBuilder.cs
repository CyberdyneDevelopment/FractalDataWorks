using System;
using System.Collections.Generic;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Builder for creating notification requests.
/// </summary>
public sealed class NotificationRequestBuilder
{
    private readonly string _channelName;
    private readonly List<string> _recipients = new();
    private string _subject = string.Empty;
    private string _message = string.Empty;
    private INotificationPriority _priority = NotificationPriorities.Normal;
    private Dictionary<string, object?>? _metadata = new(StringComparer.Ordinal);
    private string? _correlationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRequestBuilder"/> class.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    public NotificationRequestBuilder(string channelName)
    {
        _channelName = channelName;
    }

    /// <summary>
    /// Adds a recipient.
    /// </summary>
    public NotificationRequestBuilder To(string recipient)
    {
        _recipients.Add(recipient);
        return this;
    }

    /// <summary>
    /// Adds multiple recipients.
    /// </summary>
    public NotificationRequestBuilder To(IEnumerable<string> recipients)
    {
        _recipients.AddRange(recipients);
        return this;
    }

    /// <summary>
    /// Sets the subject.
    /// </summary>
    public NotificationRequestBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    /// <summary>
    /// Sets the message.
    /// </summary>
    public NotificationRequestBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    /// Sets the priority.
    /// </summary>
    public NotificationRequestBuilder WithPriority(INotificationPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Adds metadata.
    /// </summary>
    public NotificationRequestBuilder WithMetadata(string key, object? value)
    {
        _metadata ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    public NotificationRequestBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Builds the notification request.
    /// </summary>
    public NotificationRequest Build()
    {
        return new NotificationRequest(
            _channelName,
            _recipients,
            _subject,
            _message,
            _priority,
            _metadata,
            _correlationId);
    }
}
