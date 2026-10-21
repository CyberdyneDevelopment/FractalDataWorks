using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// The sources and connections behind the data sets the pipelines reach, and the connections the
/// pipelines themselves read and write. Served on its own route under <c>connections:read</c>, so
/// who sees it is the token's permission and no endpoint inspects the caller. Every edge joins a
/// node <see cref="DataflowBaseLayer"/> already draws, so a client lays this over that.
/// </summary>
internal static class DataflowSourcesLayer
{
    /// <summary>Builds the layer from the inputs.</summary>
    public static DataflowGraphResponse Build(DataflowGraphInputs inputs)
    {
        var draft = new DataflowGraphDraft();

        foreach (var owner in inputs.ReachableOwners)
        foreach (var source in owner.Sources)
            AddSource(draft, owner, source);

        foreach (var pipeline in inputs.Pipelines)
            AddPipelineConnections(draft, pipeline);

        return draft.ToResponse(new DataflowStatsDto
        {
            SourceCount = draft.Count("source"),
            ConnectionCount = draft.Count("connection")
        });
    }

    private static void AddSource(DataflowGraphDraft draft, IDataSetImplementationConfiguration owner, DataSetSourceConfiguration source)
    {
        draft.AddNode(DataflowNodes.Source(source));
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = $"edge_ds_{owner.Id}_src_{source.Id}",
            Source = DataflowNodeIds.DataSet(owner.Id),
            Target = DataflowNodeIds.Source(source.Id),
            RelationType = "uses_source",
            Label = $"Priority: {source.Priority}"
        });

        if (string.IsNullOrEmpty(source.ConnectionName))
            return;

        draft.AddNode(DataflowNodes.Connection(source.ConnectionName));
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = $"edge_src_{source.Id}_conn_{source.ConnectionName}",
            Source = DataflowNodeIds.Source(source.Id),
            Target = DataflowNodeIds.Connection(source.ConnectionName),
            RelationType = "uses_connection"
        });
    }

    private static void AddPipelineConnections(DataflowGraphDraft draft, PipelineLineageRecord pipeline)
    {
        foreach (var container in pipeline.ContainersRead)
            AddPipelineContainer(draft, pipeline, container.DataStoreName, container.ContainerName, reads: true);

        foreach (var container in pipeline.ContainersWritten)
            AddPipelineContainer(draft, pipeline, container.DataStoreName, container.ContainerName, reads: false);
    }

    // A pipeline's data store source or destination is drawn as the container it names (FDW-1269).
    private static void AddPipelineContainer(
        DataflowGraphDraft draft,
        PipelineLineageRecord pipeline,
        string? dataStoreName,
        string? containerName,
        bool reads)
    {
        if (string.IsNullOrEmpty(dataStoreName) || string.IsNullOrEmpty(containerName))
            return;

        var containerId = DataflowNodeIds.Container(dataStoreName, containerName);
        var pipelineId = DataflowNodeIds.Pipeline(pipeline.Name);
        draft.AddNode(DataflowNodes.Container(dataStoreName, containerName));
        draft.AddEdge(new DataflowEdgeDto
        {
            Id = reads ? $"edge_container_{dataStoreName}_{containerName}_pipe_{pipeline.Name}" : $"edge_pipe_{pipeline.Name}_container_{dataStoreName}_{containerName}",
            Source = reads ? containerId : pipelineId,
            Target = reads ? pipelineId : containerId,
            RelationType = reads ? "reads_from" : "writes_to"
        });
    }
}
