using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A node is consumed by a pipeline: the pipeline reads it.</summary>
[TypeOption(typeof(MapRelationOptions), "consumed_by")]
[ExcludeFromCodeCoverage]
public sealed class ConsumedByMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ConsumedByMapRelation"/> class.</summary>
    public ConsumedByMapRelation()
        : base("consumed_by")
    {
    }

    /// <inheritdoc />
    public override void Contribute(MapEdgeDto edge, ILineageFlowSink sink) => sink.Read(edge.Target, edge.Source);
}
