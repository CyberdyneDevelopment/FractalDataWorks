using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Request to create an escalation policy.
/// </summary>
public sealed class CreateEscalationPolicyRequest
{
    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the policy description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets the item type scope.</summary>
    public string? ItemType { get; set; }
    /// <summary>Gets or sets the workflow scope.</summary>
    public Guid? WorkflowId { get; set; }
    /// <summary>Gets or sets the schedule scope.</summary>
    public Guid? ScheduleId { get; set; }
    /// <summary>Gets or sets the maximum escalation level.</summary>
    public int MaxEscalationLevel { get; set; } = 3;
    /// <summary>Gets or sets the cooldown period in minutes.</summary>
    public int CooldownMinutes { get; set; } = 60;
    /// <summary>Gets or sets the condition expression.</summary>
    public string? ConditionExpression { get; set; }
    /// <summary>Gets or sets the escalation levels.</summary>
    public IReadOnlyList<EscalationLevelPayload> Levels { get; set; } = [];
}
