using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The answer of <c>POST /datasets/graph/preview</c> (FDW-5454): the draft's problems and no rows, or the rows and no problems.</summary>
public sealed class GraphDataSetPreviewPayload
{
    /// <summary>Gets or sets a value indicating whether the draft is sound; when it is not, nothing was read.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets every problem of the draft, each with its code and path, as <c>POST /datasets/graph/validate</c> answers them.</summary>
    public IList<GraphDataSetProblemPayload> Problems { get; set; } = [];

    /// <summary>Gets or sets the rows read at the stated output, each by field name; empty when the draft is not sound.</summary>
    public IList<IDictionary<string, object?>> Rows { get; set; } = [];
}
