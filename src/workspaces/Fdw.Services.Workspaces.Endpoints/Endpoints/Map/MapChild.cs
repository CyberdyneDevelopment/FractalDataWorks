namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One child of an exploded node, and the edge that joins it to the node: a unit that is sent together or not at all.</summary>
/// <param name="Node">The child's node.</param>
/// <param name="Edge">The edge between the child and the node that was exploded.</param>
internal sealed record MapChild(MapNodeDto Node, MapEdgeDto Edge);
