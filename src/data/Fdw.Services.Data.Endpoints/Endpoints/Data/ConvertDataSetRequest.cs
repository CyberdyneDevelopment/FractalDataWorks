namespace Fdw.Services.Data.Endpoints;

/// <summary>The request of <c>PATCH /datasets/{name}/convert</c> (FDW-6972): which data set becomes a Graph data set, and how its source reads.</summary>
public sealed class ConvertDataSetRequest
{
    /// <summary>Gets or sets the name of the data set to convert, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many rows the Graph data set's Source component reads in a batch: the caller's choice, at least one. Required: a Simple data
    /// set has no batch size to carry, and none is assumed.
    /// </summary>
    public int? BatchSize { get; set; }
}
