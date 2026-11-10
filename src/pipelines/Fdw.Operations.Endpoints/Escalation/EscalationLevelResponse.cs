using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// DTO for an escalation level within a policy.
/// </summary>
public class EscalationLevelResponse
{
    /// <summary>Gets or sets the escalation level number.</summary>
    public int Level { get; set; }

    /// <summary>Gets or sets the level's name. Required on a request: the caller states it, there is no default (FDW-4852).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the delay in minutes.</summary>
    public int? DelayMinutes { get; set; }

    /// <summary>Gets or sets the notification channel.</summary>
    public string NotificationChannel { get; set; } = string.Empty;

    /// <summary>Gets or sets the recipients, each with its own type. Empty for a level on a channel that needs none.</summary>
    public IReadOnlyList<EscalationRecipientResponse> Recipients { get; set; } = [];

    /// <summary>Gets or sets the message template.</summary>
    public string? MessageTemplate { get; set; }

    /// <summary>Gets or sets the severity of the notification at this level. Required on a request.</summary>
    public string Severity { get; set; } = string.Empty;
}
