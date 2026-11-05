using System;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Request naming a threshold and the subject it is attached to or detached from (FDW-3948): the route of the attach and detach endpoints.</summary>
public sealed class ThresholdSubjectRequest
{
    /// <summary>Gets or sets the threshold's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of subject, a name of ThresholdSubjectKinds, from the route.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject's Id in its domain table, from the route.</summary>
    public Guid SubjectId { get; set; }
}
