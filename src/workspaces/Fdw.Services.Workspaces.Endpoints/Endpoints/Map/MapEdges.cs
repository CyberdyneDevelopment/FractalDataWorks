using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Builds the edges of the map from what the nodes sent recorded.</summary>
/// <remarks>
/// An edge is built only when both of its ends are nodes the stream sent, so a client is never handed an edge to
/// something it does not hold. A reference to an object outside the scope is not an error: the scope is what leaves it off.
/// </remarks>
internal static class MapEdges
{
    /// <summary>The edges of the data sets: where each is stored, and what each is derived from.</summary>
    /// <param name="state">What the nodes sent recorded.</param>
    public static IEnumerable<MapEdgeDto> ForDataSets(MapState state)
        => state.DataSets.SelectMany(dataSet => StoredIn(state, dataSet).Concat(DerivedFrom(state, dataSet)));

    /// <summary>The edges of the pipelines: the data set each reads, and the data set each writes.</summary>
    /// <param name="state">What the nodes sent recorded.</param>
    public static IEnumerable<MapEdgeDto> ForPipelines(MapState state)
        => state.Pipelines.SelectMany(pipeline => Consumes(state, pipeline).Concat(Produces(state, pipeline)));

    /// <summary>The edge that leaves one node for another.</summary>
    /// <param name="relationType">One of <see cref="MapRelations"/>.</param>
    /// <param name="source">The id of the node the edge leaves.</param>
    /// <param name="target">The id of the node the edge reaches.</param>
    public static MapEdgeDto Between(string relationType, string source, string target) => new()
    {
        Id = string.Create(CultureInfo.InvariantCulture, $"{relationType}:{source}>{target}"),
        Source = source,
        Target = target,
        RelationType = relationType,
    };

    /// <summary>The edge for a join a workspace declares between two data sets.</summary>
    /// <param name="relationship">The declared relationship.</param>
    /// <remarks>Its id is the relationship's own: two data sets can be joined on different fields by two relationships.</remarks>
    public static MapEdgeDto Declared(WorkspaceRelationshipConfiguration relationship) => new()
    {
        Id = string.Create(CultureInfo.InvariantCulture, $"{MapRelations.RelatesTo}:{relationship.Id:D}"),
        Source = MapNodeIds.DataSet(relationship.LeftDataSetId),
        Target = MapNodeIds.DataSet(relationship.RightDataSetId),
        RelationType = MapRelations.RelatesTo,
        Label = relationship.Name,
        Metadata = DeclaredMetadata(relationship),
    };

    private static IEnumerable<MapEdgeDto> StoredIn(MapState state, MapDataSetLinks dataSet)
        => dataSet.DataStoreNames
            .Where(name => state.DataStoresByName.ContainsKey(name))
            .Select(name => Between(MapRelations.StoredIn, MapNodeIds.DataSet(dataSet.Id), MapNodeIds.DataStore(state.DataStoresByName[name])));

    // A source names its origin by id, by name, or both: the pipeline reachability rule reads either, so the edge does too.
    private static IEnumerable<MapEdgeDto> DerivedFrom(MapState state, MapDataSetLinks dataSet)
        => dataSet.DerivedFromIds
            .Concat(dataSet.DerivedFromNames.Where(name => state.DataSetsByName.ContainsKey(name)).Select(name => state.DataSetsByName[name]))
            .Distinct()
            .Where(origin => origin != dataSet.Id && state.HasNode(MapNodeIds.DataSet(origin)))
            .Select(origin => Between(MapRelations.DerivedFrom, MapNodeIds.DataSet(dataSet.Id), MapNodeIds.DataSet(origin)));

    private static IEnumerable<MapEdgeDto> Consumes(MapState state, MapPipelineLinks pipeline)
        => Named(state, pipeline.SourceDataSetNames)
            .Select(dataSet => Between(MapRelations.ConsumedBy, MapNodeIds.DataSet(dataSet), MapNodeIds.Pipeline(pipeline.Id)));

    private static IEnumerable<MapEdgeDto> Produces(MapState state, MapPipelineLinks pipeline)
        => Named(state, pipeline.DestinationDataSetNames)
            .Select(dataSet => Between(MapRelations.Produces, MapNodeIds.Pipeline(pipeline.Id), MapNodeIds.DataSet(dataSet)));

    // A name the scope holds no data set for draws no edge; the same data set named twice draws one.
    private static IEnumerable<Guid> Named(MapState state, IReadOnlyList<string> dataSetNames)
        => dataSetNames
            .Where(name => !string.IsNullOrEmpty(name) && state.DataSetsByName.ContainsKey(name))
            .Select(name => state.DataSetsByName[name])
            .Distinct();

    private static Dictionary<string, object> DeclaredMetadata(WorkspaceRelationshipConfiguration relationship)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["cardinality"] = relationship.Cardinality,

            // Both sides must name a field for the join to be defined. Read from the stored nulls, not inferred.
            ["isDefined"] = relationship.LeftFieldId.HasValue && relationship.RightFieldId.HasValue,
        };

        // Omitted rather than emitted as empty when unset, so a client cannot read a placeholder as a chosen field.
        if (relationship.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString("D", CultureInfo.InvariantCulture);
        if (relationship.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString("D", CultureInfo.InvariantCulture);
        return metadata;
    }
}
