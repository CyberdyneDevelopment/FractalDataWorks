using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Execution;

/// <summary>Reads a pipeline's configuration and disables the pipeline, for the failure rule (FDW-3864).</summary>
public interface IPipelineFailureConfigurations
{
    /// <summary>Reads the pipeline.</summary>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The graph pipeline; null when the pipeline is not a graph; a failure when it cannot be read.</returns>
    Task<IGenericResult<GraphPipelineConfiguration?>> Read(string pipelineName, CancellationToken cancellationToken = default);

    /// <summary>Saves the pipeline with <see cref="GraphPipelineConfiguration.IsEnabled"/> false.</summary>
    /// <param name="graph">The pipeline as read; it is changed to disabled.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    Task<IGenericResult> Disable(GraphPipelineConfiguration graph, CancellationToken cancellationToken = default);
}
