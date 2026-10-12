using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Request to update an escalation policy by ID with body data.
/// </summary>
public class UpdateEscalationPolicyRequest
{
    /// <summary>Gets or sets the policy ID from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the policy name. Omitted leaves it unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets whether the policy is enabled. Omitted leaves it unchanged.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the optional workflow ID scope. Omitted leaves it unchanged.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the maximum escalation level. Omitted leaves it unchanged.</summary>
    public int? MaxEscalationLevel { get; set; }

    /// <summary>Gets or sets the cooldown period in minutes. Omitted leaves it unchanged.</summary>
    public int? CooldownMinutes { get; set; }

    /// <summary>
    /// Gets or sets the escalation levels. Omitted (null) leaves them unchanged; an empty list is a
    /// real request to clear them.
    /// </summary>
    public IReadOnlyList<EscalationLevelResponse>? Levels { get; set; }
}
