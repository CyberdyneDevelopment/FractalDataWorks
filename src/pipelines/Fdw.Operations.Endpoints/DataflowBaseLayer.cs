using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// The dataflow graph every reader of <c>pipelines:read</c> may see: the pipelines, the data sets
/// they reach, and where those data sets are stored. It stops at the datastore and container. The
/// sources and connections behind a data set belong to <see cref="DataflowSourcesLayer"/>, which
/// takes its own permission.
/// </summary>
internal static class DataflowBaseLayer
{
    /// <summary>Builds the layer from the inputs.</summary>
    public static DataflowGraphResponse Build(DataflowGraphInputs inputs)
    {
        var draft = new DataflowGraphDraft();

        foreach (var owner in inputs.ReachableOwners)
            draft.AddNode(DataflowNodes.DataSet(owner, inputs.CategoryPathOf(owner)));

        foreach (var pipeline in inputs.Pipelines)
            AddPipeline(inputs, draft, pipeline);

        foreach (var owner in inputs.ReachableOwners)
        foreach (var source in owner.Sources)
            AddStorage(inputs, draft, owner, source);

        foreach (var variation in inputs.Variations)
            AddVariation(draft, variation);

        return draft.ToResponse(new DataflowStatsDto
        {
            DataSetCount = draft.Count("dataset"),
            PipelineCount = draft.Count("pipeline"),
            DataStoreCount = draft.Count("datastore"),
            ContainerCount = draft.Count("container"),
            VariationCount = draft.Count("variation")
        });
    }

    private static void AddPipeline(DataflowGraphInputs inputs, DataflowGraphDraft draft, PipelineLineageRecord pipeline)
    {
        draft.AddNode(DataflowNodes.Pipeline(pipeline));
        foreach (var dataSetName in pipeline.DataSetsRead)
            AddPipelineEdge(inputs, draft, pipeline, dataSetName, consumes: true);

        foreach (var dataSetName in pipeline.DataSetsWritten)
            AddPipelineEdge(inputs, draft, pipeline, dataSetName, consumes: false);
    }

    private static void AddPipelineEdge(
        DataflowGraphInputs inputs,
        DataflowGraphDraft draft,
        PipelineLineageRecord pipeline,
        string? dataSetName,
        bool consumes)
    {
        if (string.IsNullOrEmpty(dataSetName) || inputs.ReachableNamed(dataSetName) is not { } owner)
            return;

        var dataSetId = DataflowNodeIds.DataSet(owner.Id);
        var pipelineId = DataflowNodeIds.Pipeline(pipeline.Name);
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = consumes ? $"edge_ds_{owner.Id}_pipe_{pipeline.Name}" : $"edge_pipe_{pipeline.Name}_ds_{owner.Id}",
            Source = consumes ? dataSetId : pipelineId,
            Target = consumes ? pipelineId : dataSetId,
            RelationType = consumes ? "consumed_by" : "produces"
        });
    }

    // A variation holds no sources: the data route runs the data set it varies, so the edge to that
    // data set is the whole of its lineage.
    private static void AddVariation(DataflowGraphDraft draft, DataSetVariationDomainConfiguration variation)
    {
        draft.AddNode(DataflowNodes.Variation(variation));
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = $"edge_variation_{variation.Id}_of_{variation.BaseDataSetId}",
            Source = DataflowNodeIds.Variation(variation.Id),
            Target = DataflowNodeIds.DataSet(variation.BaseDataSetId),
            RelationType = "variation_of"
        });
    }

    private static void AddStorage(
        DataflowGraphInputs inputs,
        DataflowGraphDraft draft,
        IDataSetImplementationConfiguration owner,
        DataSetSourceConfiguration source)
    {
        var store = inputs.StoreNamed(source.DataStoreName);
        if (store is not null)
            draft.AddNode(DataflowNodes.DataStore(store));

        if (string.IsNullOrEmpty(source.ContainerName))
            AddStoredIn(draft, owner, store);
        else
            AddContainer(draft, owner, store, source);
    }

    private static void AddStoredIn(DataflowGraphDraft draft, IDataSetImplementationConfiguration owner, DataStoreRecord? store)
    {
        if (store is null)
            return;

        draft.AddEdge(new DataflowEdgeDto
        {
            Id = $"edge_ds_{owner.Id}_store_{store.Id}",
            Source = DataflowNodeIds.DataSet(owner.Id),
            Target = DataflowNodeIds.DataStore(store.Id),
            RelationType = "stored_in"
        });
    }

    private static void AddContainer(
        DataflowGraphDraft draft,
        IDataSetImplementationConfiguration owner,
        DataStoreRecord? store,
        DataSetSourceConfiguration source)
    {
        var containerId = DataflowNodeIds.Container(source.DataStoreName, source.ContainerName);
        draft.AddNode(DataflowNodes.Container(source.DataStoreName, source.ContainerName));
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = $"edge_ds_{owner.Id}_{containerId}",
            Source = DataflowNodeIds.DataSet(owner.Id),
            Target = containerId,
            RelationType = "stored_in"
        });

        if (store is not null)
            draft.AddEdge(new DataflowEdgeDto
            {
                Id = $"edge_{containerId}_store_{store.Id}",
                Source = containerId,
                Target = DataflowNodeIds.DataStore(store.Id),
                RelationType = "part_of"
            });
    }
}
