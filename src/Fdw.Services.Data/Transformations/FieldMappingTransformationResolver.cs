using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Data.Transformations;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Transformations;

/// <summary>
/// Resolves, for a data-set source, the transformation chain attached to each of its field mappings
/// (<c>transform.FieldMappingTransformation</c>), in chain order.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes an attached transformation do something. The attachment rows were written by
/// the field-mapping endpoints but nothing read them when data was read (FDW-877).
/// </para>
/// <para>
/// It reads through the configuration gateway, not the data gateway, for two reasons: attachments are
/// configuration rows, and the data gateway is what calls this, so depending on it would be a cycle.
/// Any unresolvable link (a missing transformation, or a type that is not a per-field operation) fails
/// the whole resolution. Dropping the link would return untransformed data as if it had been
/// transformed.
/// </para>
/// </remarks>
public sealed class FieldMappingTransformationResolver
{
    private const string PathName = "transform";
    private const string ContainerName = "FieldMappingTransformation";

    private static readonly IFilterOperator EqualOperator = FilterOperators.ByName("Equal")
        ?? throw new InvalidOperationException("FilterOperators.Equal not found");

    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly TransformationConfigurationProvider _transformations;
    private readonly ILogger<FieldMappingTransformationResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="FieldMappingTransformationResolver"/> class.</summary>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="transformations">Resolves an attached transformation by its Id, and names its store.</param>
    /// <param name="logger">The logger.</param>
    public FieldMappingTransformationResolver(
        IConfigurationGatewayProvider gatewayProvider,
        TransformationConfigurationProvider transformations,
        ILogger<FieldMappingTransformationResolver>? logger = null)
    {
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        _logger = logger ?? NullLogger<FieldMappingTransformationResolver>.Instance;
    }

    /// <summary>
    /// Gets the chain for every field mapping of <paramref name="source"/> that has attachments, keyed by
    /// logical field name. A field with no attachments has no entry.
    /// </summary>
    /// <param name="source">The source whose stored mappings are resolved.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>> ForSource(
        DataSetSourceConfiguration source, CancellationToken cancellationToken = default)
    {
        var chains = new Dictionary<string, IReadOnlyList<ResolvedFieldTransformation>>(StringComparer.Ordinal);
        var mappings = source.Mappings.Where(m => m.IsCurrent && !m.IsDeleted).ToList();
        if (mappings.Count == 0)
            return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Success(chains);

        var gateway = _gatewayProvider.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>();

        var target = new DataStoreTarget(_transformations.DataStoreName, PathName, ContainerName);

        foreach (var mapping in mappings)
        {
            var attachments = await gateway.Value!.Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                new QueryCommand<FieldMappingTransformationConfiguration> { Filter = ActiveFor(mapping.Id) },
                target,
                cancellationToken).ConfigureAwait(false);
            if (attachments.IsFailure)
            {
                FieldMappingTransformationResolverLog.AttachmentsUnreadable(_logger, source.SourceName, mapping.LogicalFieldName);
                return attachments.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>();
            }

            var chain = new List<ResolvedFieldTransformation>();
            foreach (var attachment in attachments.Value!.OrderBy(a => a.Ordinal))
            {
                var transformation = await _transformations.Get(attachment.TransformationId, cancellationToken).ConfigureAwait(false);
                if (transformation.IsFailure || transformation.Value is null)
                {
                    return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Failure(
                        FieldMappingTransformationResolverLog.TransformationMissing(
                            _logger, mapping.LogicalFieldName, attachment.TransformationId));
                }

                if (transformation.Value is not IFieldTransformationConfiguration field)
                {
                    return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Failure(
                        FieldMappingTransformationResolverLog.TransformationNotFieldLevel(
                            _logger, mapping.LogicalFieldName, transformation.Value.Name, transformation.Value.Implementation));
                }

                if (TransformationTypes.ByName(field.TransformationType) is not FieldTransformationBase operation)
                {
                    return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Failure(
                        FieldMappingTransformationResolverLog.OperationUnknown(
                            _logger, field.Name, field.TransformationType));
                }

                chain.Add(new ResolvedFieldTransformation(
                    field.Name,
                    operation,
                    field.Parameters.ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal)));
            }

            if (chain.Count > 0)
                chains[mapping.LogicalFieldName] = chain;
        }

        FieldMappingTransformationResolverLog.Resolved(_logger, source.SourceName, chains.Count);
        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<ResolvedFieldTransformation>>>.Success(chains);
    }

    private static FilterExpression ActiveFor(Guid fieldMappingId) => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = nameof(FieldMappingTransformationConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(FieldMappingTransformationConfiguration.IsDeleted), Operator = EqualOperator, Value = false },
                new FilterCondition { PropertyName = nameof(FieldMappingTransformationConfiguration.DataSetFieldMappingId), Operator = EqualOperator, Value = fieldMappingId },
            ]
        }
    };
}
