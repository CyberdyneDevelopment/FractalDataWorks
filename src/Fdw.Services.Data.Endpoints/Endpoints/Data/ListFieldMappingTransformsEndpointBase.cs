using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing field mapping transforms by field mapping identifier.
/// Provides virtual default implementations using IDataGateway for reads.
/// </summary>
/// <remarks>
/// Why: FieldMappingTransform is a child record that does not implement IGenericConfiguration,
/// so reads go through IDataGateway directly. The configuration connection name and path are
/// sourced from DataSetConfigurationProvider rather than IConfigurationConnectionNameProvider.
/// </remarks>
public abstract class ListFieldMappingTransformsEndpointBase : CrudListEndpointBase<FieldMappingTransformPayload>
{
    /// <summary>Gets the data gateway for executing queries.</summary>
    protected IDataGateway DataGateway { get; }

    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected ListFieldMappingTransformsEndpointBase(
        IDataGatewayProvider dataGateways,
        DataSetConfigurationProvider dataSetProvider)
    {
        DataGateway = dataGateways.ByName("Main");
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "field-mapping-transforms";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <summary>Gets the route for this endpoint.</summary>
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List transforms for a field mapping";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns the ordered list of transforms configured for a specific field mapping.";

    /// <summary>Gets the connection name for configuration database queries.</summary>
    protected virtual string ConfigurationConnectionName => _dataSetProvider.DataStoreName;

    /// <summary>
    /// Gets the configuration path that holds the transform containers.
    /// </summary>
    protected virtual string TransformPathName => "transform";

    /// <summary>Gets the container name for FieldMappingTransform queries.</summary>
    protected virtual string TransformContainerName => "FieldMappingTransform";

    /// <summary>Gets the container name for FieldMappingTransformParameter queries.</summary>
    protected virtual string TransformParameterContainerName => "FieldMappingTransformParameter";

    /// <summary>Loads all transforms for the field mapping from the query string.</summary>
    protected override Task<IGenericResult<List<FieldMappingTransformPayload>>> LoadItems(CancellationToken ct)
    {
        var fieldMappingIdString = HttpContext.Request.RouteValues["FieldMappingId"]?.ToString();
        if (string.IsNullOrEmpty(fieldMappingIdString) || !Guid.TryParse(fieldMappingIdString, out var fieldMappingId))
        {
            return Task.FromResult(GenericResult<List<FieldMappingTransformPayload>>.Success(new List<FieldMappingTransformPayload>()));
        }

        return LoadTransforms(fieldMappingId, ct);
    }

    /// <summary>
    /// Loads all transforms for a field mapping and returns as DTOs ordered by ordinal.
    /// Default implementation queries via IDataGateway. Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<List<FieldMappingTransformPayload>>> LoadTransforms(Guid fieldMappingId, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.ListingTransforms(Logger);

        var transformCommand = new QueryCommand<FieldMappingTransformConfiguration>
        {
            Filter = DataSetQueryHelper.ByParentIdFilter(nameof(FieldMappingTransformConfiguration.DataSetFieldMappingId), fieldMappingId)
        };
        var transformResult = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformConfiguration>>(
                transformCommand, new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName), ct)
            .ConfigureAwait(false);
        if (transformResult.IsFailure)
        {
            FieldMappingTransformEndpointLog.ListTransformsFailed(Logger, fieldMappingId);
            return transformResult.ToNewResult<List<FieldMappingTransformPayload>>();
        }

        var transforms = transformResult.Value?.ToList() ?? [];

        // Batch-load all parameters for these transforms
        var paramCommand = new QueryCommand<FieldMappingTransformParameterConfiguration>
        {
            Filter = DataSetQueryHelper.ActiveFilter()
        };
        var paramResult = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformParameterConfiguration>>(
                paramCommand, new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformParameterContainerName), ct)
            .ConfigureAwait(false);
        var paramsByTransform = paramResult.IsSuccess
            ? paramResult.Value?.GroupBy(p => p.FieldMappingTransformId).ToDictionary(g => g.Key, g => g.ToList()) ?? new Dictionary<Guid, List<FieldMappingTransformParameterConfiguration>>()
            : new Dictionary<Guid, List<FieldMappingTransformParameterConfiguration>>();

        var dtos = transforms
            .OrderBy(t => t.Ordinal)
            .Select(t => MapToDto(t, paramsByTransform.GetValueOrDefault(t.Id, [])))
            .ToList();

        FieldMappingTransformEndpointLog.ListedTransforms(Logger, dtos.Count);
        return GenericResult<List<FieldMappingTransformPayload>>.Success(dtos);
    }

    /// <summary>Maps a FieldMappingTransformConfiguration and its parameters to a DTO.</summary>
    protected virtual FieldMappingTransformPayload MapToDto(
        FieldMappingTransformConfiguration config,
        IReadOnlyList<FieldMappingTransformParameterConfiguration> parameters)
    {
        return new FieldMappingTransformPayload
        {
            Id = config.Id,
            FieldMappingId = config.DataSetFieldMappingId,
            TransformType = config.TransformType,
            Ordinal = config.Ordinal,
            Parameters = parameters.Select(p => new FieldMappingTransformParameterPayload
            {
                Id = p.Id,
                TransformId = p.FieldMappingTransformId,
                Name = p.Name,
                Value = p.Value
            }).ToList()
        };
    }
}
