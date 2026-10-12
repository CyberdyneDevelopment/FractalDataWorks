using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Base class for Notification result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class NotificationResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected NotificationResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationResultCodeBase"/> class.
    /// </summary>
    protected NotificationResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Notifications", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationResultCodeBase"/> class with a categorized number identity.
    /// </summary>
    protected NotificationResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "NOTIFICATION", isRetryable)
    {
    }
}