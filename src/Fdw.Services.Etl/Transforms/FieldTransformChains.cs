using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Turns the chains of named Transformations that the field mappings of a pipeline's Map stages state into what a Map stage
/// applies: for every step the transformation type and its parameters, by the id of the mapping.
/// </summary>
/// <remarks>
/// Why the run resolves the chains once, when it starts: a step names a Transformation by id, and finding it needs the
/// Transformation domain, which a stage running per record cannot reach. Resolving up front also means a Transformation that
/// no longer resolves, is not a field-level transformation, or names an operation no option implements fails the run before
/// any record moves, naming the stage and the mapping. Nothing is skipped and no transformation is assumed in its place.
/// Why it reads the Transformation domain and not its own table: the chain of a DATA SET field mapping is resolved from the
/// same Transformations (FDW-846), so a Transformation edited once changes both.
/// The stages inside the paths of a Branch are searched too, however deeply they nest: the Branch runs them through the same
/// context, so a Map stage inside a path finds its chain by the id of its mapping exactly as a Map stage of the pipeline does.
/// A disabled stage is not searched, because it never runs.
/// </remarks>
public static class FieldTransformChains
{
    /// <summary>
    /// Resolves the chains stated by the field mappings of <paramref name="stages"/>.
    /// </summary>
    /// <param name="stages">The stages whose field mappings are searched, and the stages inside their paths; null for a pipeline with no stages, which the runtime reads as pass-through.</param>
    /// <param name="transformations">The Transformation provider; needed only when some mapping states a chain.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">Cancels the resolution.</param>
    /// <returns>
    /// The resolved chains by mapping id (empty when no mapping states a chain), or the failure naming the stage, the mapping
    /// and the step that could not be resolved.
    /// </returns>
    public static async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>>>> Resolve(
        IEnumerable<PipelineTransformConfiguration>? stages,
        ITransformationConfigurationProvider? transformations,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var chains = new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>();
        var failure = await ResolveStages(stages, null, chains, transformations, logger, cancellationToken).ConfigureAwait(false);
        if (failure is not null)
        {
            return failure;
        }

        FieldChainLog.ChainsResolved(logger, chains.Count, chains.Values.Sum(chain => chain.Count));
        return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>>>.Success(chains);
    }

    // Resolves the chains of one list of stages and, below them, of the stages in the paths of each stage. The name given to a
    // refusal is the stage's place the way a person reads it: stage names and path names joined by '.'. Returns the failure of
    // the first chain that cannot be resolved, or null when every chain was.
    private static async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>>>?> ResolveStages(
        IEnumerable<PipelineTransformConfiguration>? stages,
        string? parentName,
        Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>> chains,
        ITransformationConfigurationProvider? transformations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (stages is null)
        {
            return null;
        }

        foreach (var stage in stages.Where(stage => stage.IsEnabled))
        {
            var stageName = parentName is null ? stage.Name : string.Concat(parentName, ".", stage.Name);
            foreach (var mapping in stage.FieldMappings.Where(mapping => mapping.TransformationChain.Count > 0))
            {
                var chain = await ResolveMapping(stageName, mapping, transformations, logger, cancellationToken).ConfigureAwait(false);
                if (chain.IsFailure)
                {
                    return chain.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransform>>>();
                }

                chains[mapping.Id] = chain.Value!;
            }

            foreach (var path in stage.Paths)
            {
                var nested = await ResolveStages(path.Stages, string.Concat(stageName, ".", path.Name), chains, transformations, logger, cancellationToken).ConfigureAwait(false);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static async Task<IGenericResult<IReadOnlyList<IFieldMappingTransform>>> ResolveMapping(
        string stage,
        PipelineTransformFieldMappingConfiguration mapping,
        ITransformationConfigurationProvider? transformations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (transformations is null)
        {
            return GenericResult<IReadOnlyList<IFieldMappingTransform>>.Failure(
                FieldChainLog.TransformationProviderMissing(logger, stage, mapping.Name));
        }

        var chain = new List<IFieldMappingTransform>();
        foreach (var step in mapping.TransformationChain.OrderBy(step => step.Ordinal))
        {
            var resolved = await ResolveStep(stage, mapping, step, transformations, logger, cancellationToken).ConfigureAwait(false);
            if (resolved.IsFailure)
            {
                return resolved.ToNewResult<IReadOnlyList<IFieldMappingTransform>>();
            }

            chain.Add(resolved.Value!);
        }

        return GenericResult<IReadOnlyList<IFieldMappingTransform>>.Success(chain);
    }

    private static async Task<IGenericResult<IFieldMappingTransform>> ResolveStep(
        string stage,
        PipelineTransformFieldMappingConfiguration mapping,
        PipelineTransformFieldMappingTransformationConfiguration step,
        ITransformationConfigurationProvider transformations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var found = await transformations.Get(step.TransformationId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return GenericResult<IFieldMappingTransform>.Failure(
                FieldChainLog.TransformationMissing(logger, stage, mapping.Name, step.TransformationId));
        }

        if (found.Value is not IFieldTransformationConfiguration field)
        {
            return GenericResult<IFieldMappingTransform>.Failure(
                FieldChainLog.TransformationNotFieldLevel(logger, stage, mapping.Name, found.Value.Name, found.Value.Implementation));
        }

        if (TransformationTypes.ByName(field.TransformationType) is not FieldTransformationBase)
        {
            return GenericResult<IFieldMappingTransform>.Failure(
                FieldChainLog.TransformationOperationUnknown(logger, stage, mapping.Name, field.Name, field.TransformationType));
        }

        var parameters = field.Parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal);
        return GenericResult<IFieldMappingTransform>.Success(new FieldMappingTransform(field.TransformationType, step.Ordinal, parameters));
    }
}
