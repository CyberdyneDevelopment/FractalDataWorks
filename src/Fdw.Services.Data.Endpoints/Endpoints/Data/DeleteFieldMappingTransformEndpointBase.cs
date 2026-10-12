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
/// Detaches a transformation from a field mapping (FDW-846):
/// <c>DELETE /field-mappings/{FieldMappingId}/transforms/{TransformationName}</c>. The transformation
/// itself, and every other field mapping it is attached to, is untouched.
/// </summary>
public abstract class DeleteFieldMappingTransformEndpointBase : CrudDeleteEndpointBase<DeleteFieldMappingTransformRequest>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Initializes a new instance of the <see cref="DeleteFieldMappingTransformEndpointBase"/> class.</summary>
    /// <remarks>
    /// The attachments are configuration rows in the transformation store the host declared, so they are read and
    /// retired through that store's configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected DeleteFieldMappingTransformEndpointBase(ILogger<DeleteFieldMappingTransformEndpointBase> logger,
        IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations) : base(logger)
    {
        _configurationGateways = configurationGateways;
        _transformations = transformations;
    }

    /// <inheritdoc/>
    protected override string ResourceName => "field-mapping-transforms";

    /// <inheritdoc/>
    protected override string DeletePolicy => "datasets:delete";

    /// <inheritdoc/>
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms/{TransformationName}";

    /// <inheritdoc/>
    protected override string EndpointSummary => "Detach a transformation from a field mapping";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Stops applying a transformation to a field mapping. The transformation itself is not deleted.";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(DeleteFieldMappingTransformRequest request) => request.TransformationName;

    /// <inheritdoc/>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(DeleteFieldMappingTransformRequest request, CancellationToken ct)
    {
        var attachment = await FindAttachment(request, ct).ConfigureAwait(false);
        if (attachment.IsFailure)
            return attachment.ToNewResult<bool>();

        return GenericResult<bool>.Success(attachment.Value is not null);
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> Delete(DeleteFieldMappingTransformRequest request, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.DeletingTransform(Logger, request.TransformationName);

        var attachment = await FindAttachment(request, ct).ConfigureAwait(false);
        if (attachment.IsFailure)
            return attachment;
        if (attachment.Value is null)
            return GenericResult.Failure(FieldMappingTransformEndpointLog.TransformationNotAttached(
                Logger, request.FieldMappingId, request.TransformationName));

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway;

        var retired = await FieldMappingTransformations
            .Retire(gateway.Value!, _transformations.DataStoreName, attachment.Value.Id, ct)
            .ConfigureAwait(false);
        if (retired.IsFailure)
        {
            FieldMappingTransformEndpointLog.DeleteTransformFailed(Logger, request.TransformationName);
            return retired;
        }

        FieldMappingTransformEndpointLog.DeletedTransform(Logger, request.TransformationName);
        return GenericResult.Success();
    }

    // The attachment is addressed by the pair (field mapping, transformation name); its own Id is
    // only what the retire command needs.
    private async Task<IGenericResult<FieldMappingTransformationConfiguration?>> FindAttachment(
        DeleteFieldMappingTransformRequest request, CancellationToken ct)
    {
        var transformation = await _transformations.Get(request.TransformationName, ct).ConfigureAwait(false);
        if (transformation.IsFailure)
            return transformation.ToNewResult<FieldMappingTransformationConfiguration?>();
        if (transformation.Value is null)
            return GenericResult<FieldMappingTransformationConfiguration?>.Success(null);

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<FieldMappingTransformationConfiguration?>();

        var attached = await FieldMappingTransformations
            .ForFieldMapping(gateway.Value!, _transformations.DataStoreName, request.FieldMappingId, ct)
            .ConfigureAwait(false);
        if (attached.IsFailure)
            return attached.ToNewResult<FieldMappingTransformationConfiguration?>();

        return GenericResult<FieldMappingTransformationConfiguration?>.Success(
            attached.Value!.FirstOrDefault(a => a.TransformationId == transformation.Value.Id));
    }
}
