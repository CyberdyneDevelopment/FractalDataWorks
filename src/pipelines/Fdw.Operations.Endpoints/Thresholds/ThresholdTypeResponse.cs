using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A kind of threshold (FDW-3948): the subject it is attached to and the values a rule of it may name.</summary>
public sealed class ThresholdTypeResponse
{
    /// <summary>Gets or sets the type's name, which a threshold states as its type.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the threshold judges.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of subject a threshold of this type is attached to: Pipeline, Component, Connection, Schedule or DataSet.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameters a threshold of this type takes; a threshold states them, and the type says which are required.</summary>
    public IReadOnlyList<ThresholdParameterResponse> Parameters { get; set; } = [];

    /// <summary>Gets or sets the values the subject reports; a rule criteria names only these.</summary>
    public IReadOnlyList<ThresholdFieldResponse> Fields { get; set; } = [];
}
