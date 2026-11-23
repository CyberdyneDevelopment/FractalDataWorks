using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set is derived from another. Data moves with the edge, against the map's own arrow.</summary>
[TypeOption(typeof(MapRelationOptions), "derived_from")]
[ExcludeFromCodeCoverage]
public sealed class DerivedFromMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DerivedFromMapRelation"/> class.</summary>
    public DerivedFromMapRelation()
        : base("derived_from")
    {
    }

    /// <inheritdoc />
    public override void Contribute(MapEdgeDto edge, ILineageFlowSink sink) => sink.Step(edge, edge.Target, edge.Source, LineageRelations.DerivedFrom);
}
