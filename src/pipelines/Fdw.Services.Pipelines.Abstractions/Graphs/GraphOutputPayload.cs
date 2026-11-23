namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>An output a graph gives back to the pipeline that starts it, as the API states it (FDW-6136).</summary>
public class GraphOutputPayload
{
    /// <summary>Gets or sets the output's name, unique within the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the graph's variable that holds the value when the run ends.</summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type of the value, a <c>DataTypes</c> option name; the variable has the same type.</summary>
    public string DataType { get; set; } = string.Empty;
}
