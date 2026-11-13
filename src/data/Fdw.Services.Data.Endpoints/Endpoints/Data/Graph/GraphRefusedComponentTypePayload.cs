namespace Fdw.Services.Data.Endpoints;

/// <summary>A component type a Graph data set's graph refuses, as <c>GET /datasets/graph/component-types/refused</c> lists it (FDW-5455).</summary>
public sealed class GraphRefusedComponentTypePayload
{
    /// <summary>Gets or sets the name of the <c>ComponentTypes</c> option; the <c>kind</c> of a component that is refused.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the <c>GraphRefusedComponentKinds</c> family the kind is in (Destination, Action or Loop).</summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>Gets or sets the result code a save answers for the kind, for example <c>DATA-21039</c>.</summary>
    public string Code { get; set; } = string.Empty;
}
