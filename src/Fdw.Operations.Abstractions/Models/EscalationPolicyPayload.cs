using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Escalation policy summary.
/// </summary>
public sealed class EscalationPolicyPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the policy description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Gets or sets the item type scope.</summary>
    public string? ItemType { get; set; }
    /// <summary>Gets or sets the workflow scope.</summary>
    public Guid? WorkflowId { get; set; }
    /// <summary>Gets or sets the schedule scope.</summary>
    public Guid? ScheduleId { get; set; }
    /// <summary>Gets or sets the maximum escalation level.</summary>
    public int MaxEscalationLevel { get; set; }
    /// <summary>Gets or sets the cooldown period in minutes.</summary>
    public int CooldownMinutes { get; set; }
    /// <summary>Gets or sets the condition expression.</summary>
    public string? ConditionExpression { get; set; }
    /// <summary>Gets or sets the escalation levels.</summary>
    public IReadOnlyList<EscalationLevelPayload> Levels { get; set; } = [];
}
