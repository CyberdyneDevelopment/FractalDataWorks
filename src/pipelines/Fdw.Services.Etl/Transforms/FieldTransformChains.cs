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
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Turns the chains of named Transformations that the mappings on the inputs of a graph's components state into what an input applies: for every step the
/// transformation type and its parameters, by the id of the mapping.
/// </summary>
/// <remarks>
/// Why the run resolves the chains once, when it starts: a step names a Transformation by id, and finding it needs the
/// Transformation domain, which a component running per record cannot reach. Resolving up front also means a Transformation that
/// no longer resolves, is not a field-level transformation, or names an operation no option implements fails the run before
/// any record moves, naming the component and the mapping. Nothing is skipped and no transformation is assumed in its place.
/// Why it reads the Transformation domain and not its own table: the chain of a DATA SET field mapping is resolved from the
/// same Transformations (FDW-846), so a Transformation edited once changes both.
/// </remarks>
public static class FieldTransformChains
{
    /// <summary>
    /// Resolves the chains stated by the mappings of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The components whose mappings are searched; null for a graph with none.</param>
    /// <param name="transformations">The Transformation provider; needed only when some mapping states a chain.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">Cancels the resolution.</param>
    /// <returns>
    /// The resolved chains by mapping id (empty when no mapping states a chain), or the failure naming the component, the mapping
    /// and the step that could not be resolved.
    /// </returns>
    public static async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>>> Resolve(
        IEnumerable<PipelineComponentConfiguration>? components,
        ITransformationConfigurationProvider? transformations,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var chains = new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>();
        foreach (var component in components ?? [])
        {
            foreach (var mapping in component.Fields.Where(mapping => mapping.TransformationChain.Count > 0))
            {
                var chain = await ResolveMapping(component.Name, mapping, transformations, logger, cancellationToken).ConfigureAwait(false);
                if (chain.IsFailure)
                {
                    return chain.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>>();
                }

                chains[mapping.Id] = chain.Value!;
            }
        }

        FieldChainLog.ChainsResolved(logger, chains.Count, chains.Values.Sum(chain => chain.Count));
        return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>>.Success(chains);
    }

    private static async Task<IGenericResult<IReadOnlyList<IFieldMappingTransformation>>> ResolveMapping(
        string component,
        PipelineComponentFieldConfiguration mapping,
        ITransformationConfigurationProvider? transformations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (transformations is null)
        {
            return GenericResult<IReadOnlyList<IFieldMappingTransformation>>.Failure(
                FieldChainLog.TransformationProviderMissing(logger, component, mapping.TargetField));
        }

        var chain = new List<IFieldMappingTransformation>();
        foreach (var step in mapping.TransformationChain.OrderBy(step => step.Ordinal))
        {
            var resolved = await ResolveStep(component, mapping, step, transformations, logger, cancellationToken).ConfigureAwait(false);
            if (resolved.IsFailure)
            {
                return resolved.ToNewResult<IReadOnlyList<IFieldMappingTransformation>>();
            }

            chain.Add(resolved.Value!);
        }

        return GenericResult<IReadOnlyList<IFieldMappingTransformation>>.Success(chain);
    }

    private static async Task<IGenericResult<IFieldMappingTransformation>> ResolveStep(
        string component,
        PipelineComponentFieldConfiguration mapping,
        PipelineComponentFieldTransformationConfiguration step,
        ITransformationConfigurationProvider transformations,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var found = await transformations.Get(step.TransformationId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return GenericResult<IFieldMappingTransformation>.Failure(
                FieldChainLog.TransformationMissing(logger, component, mapping.TargetField, step.TransformationId));
        }

        if (found.Value is not IFieldTransformationConfiguration field)
        {
            return GenericResult<IFieldMappingTransformation>.Failure(
                FieldChainLog.TransformationNotFieldLevel(logger, component, mapping.TargetField, found.Value.Name, found.Value.Implementation));
        }

        if (TransformationTypes.ByName(field.TransformationType) is not FieldTransformationBase)
        {
            return GenericResult<IFieldMappingTransformation>.Failure(
                FieldChainLog.TransformationOperationUnknown(logger, component, mapping.TargetField, field.Name, field.TransformationType));
        }

        var parameters = field.Parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal);
        return GenericResult<IFieldMappingTransformation>.Success(new FieldMappingTransformation(field.TransformationType, step.Ordinal, parameters));
    }
}
