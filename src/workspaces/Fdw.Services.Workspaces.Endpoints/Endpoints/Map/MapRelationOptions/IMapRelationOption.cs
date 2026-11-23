using Fdw.Collections;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One relation a map edge can have, and what an edge of that relation contributes to the lineage graph.</summary>
public interface IMapRelationOption : ITypeOption<int, MapRelationOptionBase>
{
    /// <summary>Gives the lineage what an edge of this relation contributes.</summary>
    /// <param name="edge">An edge whose <see cref="MapEdgeDto.RelationType"/> is this relation.</param>
    /// <param name="sink">Takes the contribution.</param>
    void Contribute(MapEdgeDto edge, ILineageFlowSink sink);
}
