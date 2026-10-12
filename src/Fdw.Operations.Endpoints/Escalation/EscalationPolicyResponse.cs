using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// DTO for an escalation policy.
/// </summary>
public class EscalationPolicyResponse
{
    /// <summary>Gets or sets the policy ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the optional workflow ID scope.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the maximum escalation level.</summary>
    public int MaxEscalationLevel { get; set; }

    /// <summary>Gets or sets the cooldown period in minutes.</summary>
    public int CooldownMinutes { get; set; }

    /// <summary>Gets or sets the escalation levels.</summary>
    public IReadOnlyList<EscalationLevelResponse> Levels { get; set; } = [];
}
