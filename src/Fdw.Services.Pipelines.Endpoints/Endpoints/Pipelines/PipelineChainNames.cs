using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Finds the names of the Transformations that a saved pipeline's field mappings chain, so a read of the pipeline can give
/// the name beside the id.
/// </summary>
/// <remarks>
/// Why one read for all of them: a pipeline keeps a Transformation by id, a person reads it by name, and a pipeline can chain
/// many; asking the provider once for the Transformations the caller can read, and picking the ones the chains name, costs one
/// query however many steps there are. A pipeline with no chain asks nothing. A Transformation the caller cannot read, or that
/// no longer exists, is simply not in the answer; the read then shows the id and no name, and nothing is assumed in its place.
/// A failure of the read itself is not that: it is returned, so a detail is never shown with names missing for a reason the
/// caller cannot tell from "cannot read this one". Stages inside the paths of a Branch are searched, however deeply they nest.
/// </remarks>
internal static class PipelineChainNames
{
    /// <summary>
    /// Reads the names of the Transformations the pipeline's chains point at.
    /// </summary>
    /// <param name="pipeline">The saved pipeline.</param>
    /// <param name="transformations">The Transformation provider, read as the caller.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The name of each Transformation the caller can read, by its id; or the failure of the read.</returns>
    public static async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Read(
        IPipelineImplementationConfiguration pipeline,
        ITransformationConfigurationProvider transformations,
        CancellationToken cancellationToken = default)
    {
        var wanted = new HashSet<Guid>();
        if (pipeline is IEtlPipelineImplementationConfiguration { Transforms: { } stages })
        {
            Collect(stages, wanted);
        }

        if (wanted.Count == 0)
        {
            return GenericResult<IReadOnlyDictionary<Guid, string>>.Success(new Dictionary<Guid, string>());
        }

        var readable = await transformations.Get(cancellationToken).ConfigureAwait(false);
        if (!readable.IsSuccess)
        {
            return readable.ToNewResult<IReadOnlyDictionary<Guid, string>>();
        }

        return GenericResult<IReadOnlyDictionary<Guid, string>>.Success(
            readable.Value!.Where(found => wanted.Contains(found.Id)).ToDictionary(found => found.Id, found => found.Name));
    }

    private static void Collect(IEnumerable<PipelineTransformConfiguration> stages, HashSet<Guid> wanted)
    {
        foreach (var stage in stages)
        {
            foreach (var step in stage.FieldMappings.SelectMany(mapping => mapping.TransformationChain))
            {
                wanted.Add(step.TransformationId);
            }

            if (stage.ComputeTransformationId is { } computed)
            {
                wanted.Add(computed);
            }

            foreach (var path in stage.Paths)
            {
                Collect(path.Stages, wanted);
            }
        }
    }
}
