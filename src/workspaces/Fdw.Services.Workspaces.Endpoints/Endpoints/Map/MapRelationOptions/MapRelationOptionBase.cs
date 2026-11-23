using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The base of every relation a map edge can have.</summary>
[ExcludeFromCodeCoverage]
public abstract class MapRelationOptionBase : TypeOptionBase<MapRelationOptionBase>, IMapRelationOption
{
    /// <summary>Initializes a new instance of the <see cref="MapRelationOptionBase"/> class.</summary>
    /// <param name="name">The relation, as the string an edge carries.</param>
    protected MapRelationOptionBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    /// <remarks>A relation that moves no data contributes nothing to the lineage; the relations that do override this.</remarks>
    public virtual void Contribute(MapEdgeDto edge, ILineageFlowSink sink)
    {
    }
}
