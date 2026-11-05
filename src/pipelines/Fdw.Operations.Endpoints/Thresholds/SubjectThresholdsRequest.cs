using System;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Request naming a subject whose thresholds are read (FDW-3948).</summary>
public sealed class SubjectThresholdsRequest
{
    /// <summary>Gets or sets the kind of subject, a name of ThresholdSubjectKinds, from the route.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject's Id in its domain table, from the route.</summary>
    public Guid SubjectId { get; set; }
}
