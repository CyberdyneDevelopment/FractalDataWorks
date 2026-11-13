namespace Fdw.Services.Data.Endpoints;

/// <summary>Why the get-graph route did not answer the graph (FDW-5295): the result code and its message, never an empty graph in place of the answer.</summary>
public sealed class GraphDataSetRestrictionPayload
{
    /// <summary>Gets or sets the result code, for example <c>DATA-21078</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets what the code says, naming the data set; it names no source.</summary>
    public string Message { get; set; } = string.Empty;
}
