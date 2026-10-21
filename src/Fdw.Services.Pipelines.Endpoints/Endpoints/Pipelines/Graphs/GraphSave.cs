using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The save every graph write shares (FDW-1396): judge the graph, write it through the pipeline domain's provider, and answer with
/// what a later read of the same pipeline returns.
/// </summary>
public static class GraphSave
{
    /// <summary>Judges and saves a graph.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    public static async Task<IGenericResult<GraphPayload>> Save(
        PipelineServiceConfigurationProvider provider, GraphPipelineConfiguration graph, ILogger logger, CancellationToken ct)
    {
        var problems = GraphShapeValidator.Problems(graph, logger);
        if (problems.Count > 0)
        {
            return GraphRefusal.Of<GraphPayload>(string.Join("; ", problems.Select(problem => problem.Reason)));
        }

        var saved = await provider.Save(graph, "Pipeline", graph.Implementation, graph.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<GraphPayload>();
        }

        return await GraphRead.Read(provider, graph.Name, ct).ConfigureAwait(false);
    }
}
