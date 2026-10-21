namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>One input or output node of a component kind, for the console to draw and wire it from (FDW-1395).</summary>
public sealed class TypeCollectionValueNodeDto
{
    /// <summary>Gets or sets the node's name, or for a node the person names, what the node is for.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the person building the graph names each node of this kind.</summary>
    public bool IsNamedByUser { get; set; }

    /// <summary>Gets or sets the fewest nodes of this kind a component has.</summary>
    public int Min { get; set; }

    /// <summary>Gets or sets the most nodes of this kind a component has; null when there is no limit.</summary>
    public int? Max { get; set; }

    /// <summary>Gets or sets a value indicating whether the output node takes the rows the component could not process.</summary>
    public bool IsError { get; set; }

    /// <summary>Gets or sets what rows arrive at, or leave through, the node.</summary>
    public string HelpText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting of the component that names the node, when the person building the graph names it through a
    /// setting; null when the graph's links name it or the kind fixes the name.
    /// </summary>
    public string? NamedBySetting { get; set; }
}
