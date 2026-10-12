using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// Deletes a transformation (FDW-846): <c>DELETE /transforms/{Name}</c>. Refused while it is attached
/// to a field mapping, and for system-provided ones.
/// </summary>
public abstract class DeleteTransformationEndpointBase : CrudDeleteEndpointBase<TransformationNameRequest>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Initializes a new instance of the <see cref="DeleteTransformationEndpointBase"/> class.</summary>
    /// <remarks>
    /// The attachments are configuration rows in the transformation store the host declared, so they are read through
    /// that store's configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected DeleteTransformationEndpointBase(ILogger<DeleteTransformationEndpointBase> logger,
        IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations) : base(logger)
    {
        _configurationGateways = configurationGateways;
        _transformations = transformations;
    }

    /// <inheritdoc/>
    protected override string ResourceName => "transforms";

    /// <inheritdoc/>
    protected override string DeletePolicy => "datasets:delete";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(TransformationNameRequest request) => request.Name;

    /// <inheritdoc/>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(TransformationNameRequest request, CancellationToken ct)
    {
        var found = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<bool>();

        return GenericResult<bool>.Success(found.Value is not null);
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> ValidateDelete(TransformationNameRequest request, CancellationToken ct)
    {
        var found = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found;
        if (found.Value is null)
            return GenericResult.Failure(FieldMappingTransformEndpointLog.TransformationNotFound(Logger, request.Name));

        if (found.Value.IsSystemProvided)
            return GenericResult.Failure(
                FieldMappingTransformEndpointLog.SystemProvidedTransformationImmutable(Logger, request.Name));

        // Why refuse rather than cascade: detaching it silently would change what every one of those
        // field mappings produces. The caller detaches deliberately first.
        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway;

        var attached = await FieldMappingTransformations
            .ForTransformation(gateway.Value!, _transformations.DataStoreName, found.Value.Id, ct)
            .ConfigureAwait(false);
        if (attached.IsFailure)
            return attached;
        if (attached.Value!.Count > 0)
            return GenericResult.Failure(
                FieldMappingTransformEndpointLog.TransformationStillAttached(Logger, request.Name, attached.Value.Count));

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult> Delete(TransformationNameRequest request, CancellationToken ct)
    {
        var deleted = await _transformations.Delete(request.Name, ct).ConfigureAwait(false);
        if (deleted.IsFailure)
            return deleted;

        FieldMappingTransformEndpointLog.TransformationDeleted(Logger, request.Name);
        return GenericResult.Success();
    }
}
