using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Escalation level within a policy.
/// </summary>
public sealed class EscalationLevelPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the escalation level number.</summary>
    public int Level { get; set; }
    /// <summary>Gets or sets the delay in minutes before this level triggers.</summary>
    public int DelayMinutes { get; set; }
    /// <summary>Gets or sets the notification channel.</summary>
    public string NotificationChannel { get; set; } = string.Empty;
    /// <summary>Gets or sets the recipients for this level.</summary>
    public IReadOnlyList<string> Recipients { get; set; } = [];
}
