using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// Represents a level within an escalation policy.
/// </summary>
public interface IEscalationLevel
{
    /// <summary>
    /// Gets the escalation level number (1, 2, 3...).
    /// </summary>
    int Level { get; }

    /// <summary>
    /// Gets the delay in minutes before this level is triggered.
    /// </summary>
    int DelayMinutes { get; }

    /// <summary>
    /// Gets the notification channel for this level (Email, Teams, Webhook, etc.).
    /// </summary>
    string NotificationChannel { get; }

    /// <summary>
    /// Gets the list of recipients for this level.
    /// </summary>
    IReadOnlyList<string> Recipients { get; }

    /// <summary>
    /// Gets the message template for notifications at this level.
    /// </summary>
    string? MessageTemplate { get; }

    /// <summary>
    /// Gets the severity of the notification at this level (Warning, Error, Critical). The caller states it; there is no default (FDW-3823).
    /// </summary>
    string Severity { get; }

    /// <summary>
    /// Gets the type of this level's recipients (Email, Webhook, IntegrationKey). The caller states it; there is no default (FDW-3823).
    /// </summary>
    string RecipientType { get; }
}
