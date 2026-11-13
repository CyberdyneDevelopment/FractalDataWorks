namespace Fdw.Services.Data.Endpoints;

/// <summary>A component type a Graph data set's graph allows, as <c>GET /datasets/graph/component-types</c> lists it (FDW-5295).</summary>
public sealed class GraphComponentTypePayload
{
    /// <summary>Gets or sets the name of the <c>ComponentTypes</c> option; the <c>kind</c> of a component states it.</summary>
    public string Name { get; set; } = string.Empty;
}
