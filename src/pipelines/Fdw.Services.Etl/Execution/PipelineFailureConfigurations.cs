using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines;

namespace Fdw.Services.Etl.Execution;

/// <summary>Reads and disables a pipeline through the pipeline configuration provider (FDW-3864).</summary>
public sealed class PipelineFailureConfigurations : IPipelineFailureConfigurations
{
    private readonly PipelineServiceConfigurationProvider _pipelines;

    /// <summary>Initializes a new instance of the <see cref="PipelineFailureConfigurations"/> class.</summary>
    public PipelineFailureConfigurations(PipelineServiceConfigurationProvider pipelines)
    {
        _pipelines = pipelines;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<GraphPipelineConfiguration?>> Read(string pipelineName, CancellationToken cancellationToken = default)
    {
        var read = await _pipelines.Get(pipelineName, cancellationToken).ConfigureAwait(false);
        return read.IsFailure
            ? read.ToNewResult<GraphPipelineConfiguration?>()
            : GenericResult<GraphPipelineConfiguration?>.Success(read.Value as GraphPipelineConfiguration);
    }

    /// <inheritdoc />
    public Task<IGenericResult> Disable(GraphPipelineConfiguration graph, CancellationToken cancellationToken = default)
    {
        graph.IsEnabled = false;
        return _pipelines.Update(graph, "Pipeline", graph.Implementation, graph.Name, cancellationToken);
    }
}
