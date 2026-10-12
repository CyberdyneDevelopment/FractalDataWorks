using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a walk of the lineage graph found: the nodes in order, the steps between them, and whether the cap cut it short.</summary>
internal sealed record LineageWalk(List<LineagePlacedNode> Nodes, List<LineageFlow> Flows, bool Truncated);
