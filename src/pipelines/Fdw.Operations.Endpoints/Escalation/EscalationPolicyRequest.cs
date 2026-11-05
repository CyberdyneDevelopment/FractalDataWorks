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

    /// <summary>Gets or sets whether the policy is enabled. Required: the caller chooses, there is no default (FDW-3823).</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the optional workflow ID scope.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the maximum escalation level. Required (FDW-3823).</summary>
    public int? MaxEscalationLevel { get; set; }

    /// <summary>Gets or sets the cooldown period in minutes. Required (FDW-3823).</summary>
    public int? CooldownMinutes { get; set; }

    /// <summary>Gets or sets how many times a failed notification is retried. Required (FDW-3823).</summary>
    public int? MaxRetries { get; set; }

    /// <summary>Gets or sets the seconds between retries. Required (FDW-3823).</summary>
    public int? RetryDelaySeconds { get; set; }

    /// <summary>Gets or sets the escalation levels.</summary>
    public IReadOnlyList<EscalationLevelResponse> Levels { get; set; } = [];
}
