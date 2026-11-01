namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Where a field at a node of a graph came from (FDW-2156): the field of a data set a source read.</summary>
public class GraphFieldOriginPayload
{
    /// <summary>Gets or sets the data set the field was read from.</summary>
    public string DataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the field of that data set.</summary>
    public string Field { get; set; } = string.Empty;
}
