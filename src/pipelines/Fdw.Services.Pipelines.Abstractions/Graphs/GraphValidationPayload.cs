using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to a check of a graph draft (FDW-1396): every problem found, or none.</summary>
public class GraphValidationPayload
{
    /// <summary>Gets or sets a value indicating whether saving the draft would be accepted.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets every problem found; empty when the draft is valid.</summary>
    public IList<GraphProblemPayload> Problems { get; set; } = [];

    /// <summary>
    /// Gets or sets what the check could not judge (FDW-5457): a SQL step on a data store whose scripts this host cannot read, names outside the data
    /// store, or a data store not yet discovered. A warning does not stop a save; empty when there is none.
    /// </summary>
    public IList<GraphProblemPayload> Warnings { get; set; } = [];
}
