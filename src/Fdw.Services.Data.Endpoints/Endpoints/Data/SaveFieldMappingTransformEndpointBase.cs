using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Transformations;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Attaches an existing transformation to a field mapping at a position in its chain (FDW-846):
/// <c>POST /field-mappings/{FieldMappingId}/transforms</c>. Define transformations at <c>/transforms</c>.
/// </summary>
public abstract class SaveFieldMappingTransformEndpointBase : CrudCreateEndpointBase<SaveFieldMappingTransformRequest, FieldMappingTransformPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Initializes a new instance of the <see cref="SaveFieldMappingTransformEndpointBase"/> class.</summary>
    /// <remarks>
    /// The attachments are configuration rows in the transformation store the host declared, so they are written
    /// through that store's configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected SaveFieldMappingTransformEndpointBase(ILogger<SaveFieldMappingTransformEndpointBase> logger,
        IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations) : base(logger)
    {
        _configurationGateways = configurationGateways;
        _transformations = transformations;
    }

    /// <inheritdoc/>
    protected override string ResourceName => "field-mapping-transforms";

    /// <inheritdoc/>
    protected override string WritePolicy => "datasets:write";

    /// <inheritdoc/>
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms";

    /// <inheritdoc/>
    protected override string EndpointSummary => "Attach a transformation to a field mapping";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Applies an existing transformation (see /transforms) to a field mapping at the given position in its chain.";

    /// <inheritdoc/>
    protected override string GetResourceName(SaveFieldMappingTransformRequest request) => request.TransformationName;

    /// <inheritdoc/>
    protected override async Task<IGenericResult<bool>> CheckExists(SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        var transformation = await _transformations.Get(request.TransformationName, ct).ConfigureAwait(false);
        if (transformation.IsFailure)
            return transformation.ToNewResult<bool>();
        if (transformation.Value is null)
            return GenericResult<bool>.Success(false);

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<bool>();

        var attached = await FieldMappingTransformations
            .ForFieldMapping(gateway.Value!, _transformations.DataStoreName, request.FieldMappingId, ct)
            .ConfigureAwait(false);
        if (attached.IsFailure)
            return attached.ToNewResult<bool>();

        return GenericResult<bool>.Success(attached.Value!.Any(a => a.TransformationId == transformation.Value.Id));
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> ValidateCreate(SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        var transformation = await _transformations.Get(request.TransformationName, ct).ConfigureAwait(false);
        if (transformation.IsFailure)
            return transformation;

        if (transformation.Value is null)
            return GenericResult.Failure(FieldMappingTransformEndpointLog.TransformationNotFound(Logger, request.TransformationName));

        // Only a transformation of one column attaches to a field mapping; the set kinds run in pipeline stages.
        return transformation.Value is IFieldTransformationConfiguration
            ? GenericResult.Success()
            : GenericResult.Failure(FieldMappingTransformEndpointLog.TransformationNotFieldLevel(
                Logger, request.TransformationName, transformation.Value.Implementation));
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<FieldMappingTransformPayload>> Create(
        SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.SavingTransform(Logger, request.TransformationName);

        var transformation = await _transformations.Get(request.TransformationName, ct).ConfigureAwait(false);
        if (transformation.IsFailure)
            return transformation.ToNewResult<FieldMappingTransformPayload>();
        if (transformation.Value is null)
            return GenericResult<FieldMappingTransformPayload>.Failure(
                FieldMappingTransformEndpointLog.TransformationNotFound(Logger, request.TransformationName));
        if (transformation.Value is not IFieldTransformationConfiguration field)
            return GenericResult<FieldMappingTransformPayload>.Failure(
                FieldMappingTransformEndpointLog.TransformationNotFieldLevel(
                    Logger, request.TransformationName, transformation.Value.Implementation));

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<FieldMappingTransformPayload>();

        var saved = await FieldMappingTransformations
            .Save(gateway.Value!, _transformations.DataStoreName, new FieldMappingTransformationConfiguration
            {
                Id = Guid.CreateVersion7(),
                DataSetFieldMappingId = request.FieldMappingId,
                TransformationId = field.Id,
                Ordinal = request.Ordinal
            }, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure)
        {
            FieldMappingTransformEndpointLog.SaveTransformFailed(Logger, request.TransformationName);
            return saved.ToNewResult<FieldMappingTransformPayload>();
        }

        FieldMappingTransformEndpointLog.SavedTransform(Logger, request.TransformationName);
        return GenericResult<FieldMappingTransformPayload>.Success(new FieldMappingTransformPayload
        {
            FieldMappingId = request.FieldMappingId,
            TransformationName = field.Name,
            TransformationType = field.TransformationType,
            Ordinal = request.Ordinal,
            IsSystemProvided = field.IsSystemProvided,
            Parameters = TransformationEndpointSupport.ToParameterPayloads(field.Parameters)
        });
    }
}
