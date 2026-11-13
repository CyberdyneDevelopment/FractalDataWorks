using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The answer to a check of a Graph data set draft (FDW-5290): every problem found, and the intended sources not bound yet.</summary>
public sealed class GraphDataSetValidationPayload
{
    /// <summary>Gets or sets a value indicating whether saving the draft would be accepted.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets every problem found; empty when the draft is valid.</summary>
    public IList<GraphDataSetProblemPayload> Problems { get; set; } = [];

    /// <summary>Gets or sets the intended sources, each not bound yet; they are information and never make the draft invalid.</summary>
    public IList<GraphDataSetNotBoundPayload> NotBound { get; set; } = [];

    /// <summary>Gets or sets what the check could not judge (FDW-5522): a coded note for each field out whose feeding output has no known fields. Information only: it never makes the draft invalid and save does not read it.</summary>
    public IList<GraphDataSetProblemPayload> NotChecked { get; set; } = [];
}
