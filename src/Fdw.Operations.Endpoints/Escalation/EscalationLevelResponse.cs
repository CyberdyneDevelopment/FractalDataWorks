using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// DTO for an escalation level within a policy.
/// </summary>
public class EscalationLevelResponse
{
    /// <summary>Gets or sets the escalation level number.</summary>
    public int Level { get; set; }

    /// <summary>Gets or sets the delay in minutes.</summary>
    public int DelayMinutes { get; set; }

    /// <summary>Gets or sets the notification channel.</summary>
    public string NotificationChannel { get; set; } = string.Empty;

    /// <summary>Gets or sets the recipients.</summary>
    public IReadOnlyList<string> Recipients { get; set; } = [];

    /// <summary>Gets or sets the message template.</summary>
    public string? MessageTemplate { get; set; }
}
