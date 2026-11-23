using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The graph pipeline layer (FDW-6131): the graph any graph pipeline type holds — its components, the lines between them, the outputs deliberately left
/// unlinked, its variables and parameters, and how many batches a line buffers. Storing, judging (<see cref="GraphShapeValidator.GraphProblems"/>,
/// <see cref="GraphComponentValidator"/>), describing (<c>GraphDescriber</c>) and building (<c>GraphFlowBuilder</c>) a graph read it through this, so a
/// second graph pipeline type is judged, described and built by the same code.
/// </summary>
/// <remarks>
/// What a pipeline does with its runs (how many run at once, what happens when that many already run) is the pipeline's, not the graph's, and stays on
/// the implementation row that carries the graph (<see cref="GraphPipelineConfiguration"/>).
/// </remarks>
public interface IGraphPipelineConfiguration
{
    /// <summary>Gets the name of the pipeline the graph belongs to, which the graph's problems name.</summary>
    string Name { get; }

    /// <summary>Gets the components: sources, transformations, joins, destinations, actions and loops.</summary>
    IList<PipelineComponentConfiguration> Components { get; }

    /// <summary>Gets the lines from one component's output to another's input, and the completion lines that leave a component's <c>done</c> output.</summary>
    IList<PipelineLinkConfiguration> Links { get; }

    /// <summary>Gets the outputs deliberately left unlinked.</summary>
    IList<PipelineDroppedOutputConfiguration> DroppedOutputs { get; }

    /// <summary>Gets the variables the graph declares.</summary>
    IList<PipelineVariableConfiguration> Variables { get; }

    /// <summary>Gets the outputs the graph gives back to the pipeline that starts it (FDW-6136).</summary>
    IList<PipelineOutputConfiguration> Outputs { get; }

    /// <summary>Gets the run parameters the graph declares.</summary>
    IList<PipelineParameterConfiguration> Parameters { get; }

    /// <summary>Gets how many batches a line buffers.</summary>
    int LinkCapacity { get; }

    /// <summary>Gets the most steps a run of the graph may take (FDW-6141), or <see langword="null"/> when the graph states none.</summary>
    int? MaxSteps { get; }
}
