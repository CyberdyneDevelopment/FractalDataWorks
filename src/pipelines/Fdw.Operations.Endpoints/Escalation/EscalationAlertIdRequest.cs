using System;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>Identifies an escalation alert by its id in the route (FDW-3863).</summary>
public sealed class EscalationAlertIdRequest
{
    /// <summary>Gets or sets the alert's id.</summary>
    public Guid Id { get; set; }
}
