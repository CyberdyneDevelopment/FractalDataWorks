using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A pipeline produces a node: the pipeline writes it.</summary>
[TypeOption(typeof(MapRelationOptions), "produces")]
[ExcludeFromCodeCoverage]
public sealed class ProducesMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ProducesMapRelation"/> class.</summary>
    public ProducesMapRelation()
        : base("produces")
    {
    }

    /// <inheritdoc />
    public override void Contribute(MapEdgeDto edge, ILineageFlowSink sink) => sink.Write(edge.Source, edge.Target);
}
