namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A field a row has at a node of a graph (FDW-1397).</summary>
public class GraphFieldPayload
{
    /// <summary>Gets or sets the field's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's type name; null when it cannot be known before a run.</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the field of a data set this one came from (FDW-2156); null when it came from none or cannot be followed.</summary>
    public GraphFieldOriginPayload? Origin { get; set; }
}
