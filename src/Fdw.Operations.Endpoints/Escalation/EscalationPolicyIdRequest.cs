using System;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Request to identify an escalation policy by ID.
/// </summary>
public class EscalationPolicyIdRequest
{
    /// <summary>Gets or sets the policy ID.</summary>
    public Guid Id { get; set; }
}
