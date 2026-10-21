namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A parameter a graph declares at design time, as the API states it (FDW-1549).</summary>
public class GraphParameterPayload
{
    /// <summary>Gets or sets the parameter's name, unique among the graph's parameters and variables.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type of its value, a <c>DataTypes</c> option name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a run must supply the parameter; an optional one a run leaves blank is absent.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets a value indicating whether no step may change the parameter once the run has started.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Gets or sets a value indicating whether the parameter holds a list: <c>DataType</c> is then the type of each item, and a run supplies a JSON array of them.</summary>
    public bool IsList { get; set; }

    /// <summary>Gets or sets a value indicating whether the value a run is given may be shown in the run's record; the author opts in by stating true, and leaving it out is false (FDW-1756).</summary>
    public bool ShowsValue { get; set; }

    /// <summary>Gets or sets the value, as text of the parameter's own type, that a run which does not give the parameter uses (FDW-1975); null when it has no default. A required or list parameter has none.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets what the parameter is for, shown to whoever starts or schedules the run; null when nothing was written.</summary>
    public string? Description { get; set; }
}
