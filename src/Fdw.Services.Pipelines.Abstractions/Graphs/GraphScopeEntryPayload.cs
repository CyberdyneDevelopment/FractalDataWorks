namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A parameter or variable a graph puts in scope for its steps, as describing the graph lists it (FDW-1549).</summary>
public class GraphScopeEntryPayload
{
    /// <summary>Gets or sets the name a step writes between double braces to read it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type of its value, a <c>DataTypes</c> option name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the entry is a parameter a run supplies; false for a variable the run sets.</summary>
    public bool IsParameter { get; set; }

    /// <summary>Gets or sets a value indicating whether no step may change it; only a parameter can be read-only.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Gets or sets a value indicating whether a run must supply it; only a parameter can be required.</summary>
    public bool IsRequired { get; set; }
}
