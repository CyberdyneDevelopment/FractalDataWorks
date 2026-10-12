namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A node and where it sits from the focus: negative upstream, zero at the focus, positive downstream.</summary>
internal sealed record LineagePlacedNode(LineageNode Node, int Depth);
