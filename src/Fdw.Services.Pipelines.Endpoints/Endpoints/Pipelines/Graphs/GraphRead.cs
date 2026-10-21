using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads a graph pipeline by name (FDW-1396).
/// </summary>
public static class GraphRead
{
    /// <summary>Reads a graph.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="name">The pipeline's name.</param>
    /// <param name="ct">Cancels the read.</param>
    /// <returns>
    /// The graph; success with no value when no pipeline has the name; the failure when the pipeline cannot be read; a refusal
    /// when the pipeline is not a graph, so a linear pipeline is never answered as an empty one.
    /// </returns>
    public static async Task<IGenericResult<GraphPayload?>> Find(PipelineServiceConfigurationProvider provider, string name, CancellationToken ct)
    {
        var read = await provider.Get(name, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<GraphPayload?>();
        }

        if (read.Value is null)
        {
            return GenericResult<GraphPayload?>.Success(null);
        }

        return read.Value is GraphPipelineConfiguration graph
            ? GenericResult<GraphPayload?>.Success(GraphPayloadWriter.Write(graph))
            : GraphRefusal.Of<GraphPayload?>($"the pipeline '{name}' is a {read.Value.Implementation} pipeline, not a graph");
    }

    /// <summary>Reads a graph that was just written.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="name">The pipeline's name.</param>
    /// <param name="ct">Cancels the read.</param>
    /// <returns>The graph, or the failure to read it back.</returns>
    public static async Task<IGenericResult<GraphPayload>> Read(PipelineServiceConfigurationProvider provider, string name, CancellationToken ct)
    {
        var found = await Find(provider, name, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<GraphPayload>();
        }

        return found.Value is { } payload
            ? GenericResult<GraphPayload>.Success(payload)
            : GraphRefusal.Of<GraphPayload>($"the pipeline '{name}' was saved and cannot be read back");
    }
}
