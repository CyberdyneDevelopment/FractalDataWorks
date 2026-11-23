using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set is stored in a data store. Data moves with the edge, against the map's own arrow.</summary>
[TypeOption(typeof(MapRelationOptions), "stored_in")]
[ExcludeFromCodeCoverage]
public sealed class StoredInMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="StoredInMapRelation"/> class.</summary>
    public StoredInMapRelation()
        : base("stored_in")
    {
    }

    /// <inheritdoc />
    public override void Contribute(MapEdgeDto edge, ILineageFlowSink sink) => sink.Step(edge, edge.Target, edge.Source, LineageRelations.StoredIn);
}
