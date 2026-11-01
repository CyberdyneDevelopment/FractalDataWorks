using Fdw.Services.Workspaces.Lineage;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A neighbourhood request that passed validation.</summary>
internal sealed record LineageNeighbourhoodQuery(string Node, ILineageDirection Direction, int Depth, LineageKindSelection Kinds);
