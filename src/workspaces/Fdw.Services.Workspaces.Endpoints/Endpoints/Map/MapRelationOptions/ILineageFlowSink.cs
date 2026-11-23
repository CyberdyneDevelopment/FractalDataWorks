namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Takes what one map edge contributes to the lineage graph.</summary>
/// <remarks>Public only because <see cref="IMapRelationOption.Contribute"/> takes it; the lineage projection is the one implementation.</remarks>
public interface ILineageFlowSink
{
    /// <summary>Records that a pipeline reads a node.</summary>
    /// <param name="pipelineId">The pipeline's node id.</param>
    /// <param name="inputId">The node id it reads.</param>
    void Read(string pipelineId, string inputId);

    /// <summary>Records that a pipeline writes a node.</summary>
    /// <param name="pipelineId">The pipeline's node id.</param>
    /// <param name="outputId">The node id it writes.</param>
    void Write(string pipelineId, string outputId);

    /// <summary>Records a step the edge itself is, which carries the edge's own marks.</summary>
    /// <param name="edge">The edge, for the marks its metadata states.</param>
    /// <param name="from">The node data moves from.</param>
    /// <param name="to">The node data moves to.</param>
    /// <param name="relation">The lineage relation the step has.</param>
    void Step(MapEdgeDto edge, string from, string to, string relation);
}
