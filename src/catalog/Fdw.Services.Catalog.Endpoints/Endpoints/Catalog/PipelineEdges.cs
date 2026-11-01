using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists what each graph pipeline uses, read from the pipelines themselves (FDW-1683, FDW-1705).</summary>
internal static class PipelineEdges
{
    /// <summary>Reads every pipeline whole and lists what each graph pipeline reads, writes, calls and runs.</summary>
    /// <param name="provider">The pipeline configuration provider.</param>
    /// <param name="transformations">The transformation configuration provider: a tool names its transformation by id, and this gives the name.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The edges, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEdge>>> Read(
        IPipelineConfigurationProvider provider, ITransformationConfigurationProvider transformations, CancellationToken cancellationToken)
    {
        // Why whole: what a pipeline uses lives in its steps and their components, which a read of the record alone does not hold.
        var read = await provider.Get(ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEdge>>();
        }

        var named = await transformations.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (named.IsFailure)
        {
            return named.ToNewResult<List<CatalogEdge>>();
        }

        var names = named.Value!.ToDictionary(transformation => transformation.Id, transformation => transformation.Name);
        return GenericResult<List<CatalogEdge>>.Success(read.Value!
            .OfType<GraphPipelineConfiguration>()
            .SelectMany(graph => GraphPipelineUses.Of(graph, names).Select(use => new CatalogEdge(CatalogKinds.Pipeline, graph.Name, use.Relation, use.Kind, use.Name, use.Parents)))
            .ToList());
    }
}
