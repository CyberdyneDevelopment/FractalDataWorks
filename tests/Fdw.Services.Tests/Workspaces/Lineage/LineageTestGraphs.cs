using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces.Endpoints;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>Builds small lineage graphs from the map's own node and edge shapes, as the endpoints read them.</summary>
internal static class LineageTestGraphs
{
    public static string DataSetId(int n) => MapNodeIds.DataSet(Guid.Parse($"00000000-0000-0000-0000-{n:D12}"));

    public static string StoreId(int n) => MapNodeIds.DataStore(Guid.Parse($"00000000-0000-0000-0001-{n:D12}"));

    public static string PipelineId(int n) => MapNodeIds.Pipeline(Guid.Parse($"00000000-0000-0000-0002-{n:D12}"));

    public static MapNodeDto NewDataSet(int n, string name) => new() { Id = DataSetId(n), Label = name, Kind = MapKinds.DataSet };

    public static MapNodeDto NewStore(int n, string name) => new() { Id = StoreId(n), Label = name, Kind = MapKinds.DataStore };

    public static MapNodeDto NewPipeline(int n, string name, bool canEdit = true) => new()
    {
        Id = PipelineId(n),
        Label = name,
        Kind = MapKinds.Pipeline,
        CanEdit = canEdit,
        Metadata = new Dictionary<string, object>(StringComparer.Ordinal) { ["isEnabled"] = true },
    };

    public static MapEdgeDto Reads(string dataSet, string pipeline) => MapEdges.Between(MapRelations.ConsumedBy, dataSet, pipeline);

    public static MapEdgeDto Writes(string pipeline, string dataSet) => MapEdges.Between(MapRelations.Produces, pipeline, dataSet);

    public static MapEdgeDto DerivedFrom(string derived, string origin) => MapEdges.Between(MapRelations.DerivedFrom, derived, origin);

    public static MapEdgeDto StoredIn(string dataSet, string store) => MapEdges.Between(MapRelations.StoredIn, dataSet, store);

    /// <summary>Raw (feed) to Landing through the Load pipeline, then Landing to Stats through the Roll pipeline.</summary>
    public static LineageGraph TwoStepChain() => LineageProjection.Of(
        [NewDataSet(1, "Feed"), NewDataSet(2, "Landing"), NewDataSet(3, "Stats"), NewPipeline(1, "Load"), NewPipeline(2, "Roll")],
        [Reads(DataSetId(1), PipelineId(1)), Writes(PipelineId(1), DataSetId(2)), Reads(DataSetId(2), PipelineId(2)), Writes(PipelineId(2), DataSetId(3))],
        null);

    public static LineageKindSelection KindsOf(string kinds, bool pipelinesAreNodes = false)
        => LineageKindSelection.Parse(kinds, "DataSet", pipelinesAreNodes, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Value!;

    public static List<string> Ids(LineageWalk walk) => walk.Nodes.Select(placed => placed.Node.Id).ToList();
}
