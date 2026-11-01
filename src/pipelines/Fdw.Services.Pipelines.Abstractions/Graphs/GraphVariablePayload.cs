using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A variable a graph declares, as the API states it (FDW-1403).</summary>
public class GraphVariablePayload
{
    /// <summary>Gets or sets the variable's name, unique within the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type of its value, a <c>DataTypes</c> option name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the starting value as text; null starts the variable null.</summary>
    public string? Value { get; set; }
}
