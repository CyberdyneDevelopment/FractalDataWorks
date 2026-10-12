using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Request for creating an escalation policy.
/// </summary>
public class EscalationPolicyRequest
{
    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the optional workflow ID scope.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the maximum escalation level.</summary>
    public int MaxEscalationLevel { get; set; }

    /// <summary>Gets or sets the cooldown period in minutes.</summary>
    public int CooldownMinutes { get; set; } = 30;

    /// <summary>Gets or sets the escalation levels.</summary>
    public IReadOnlyList<EscalationLevelResponse> Levels { get; set; } = [];
}
