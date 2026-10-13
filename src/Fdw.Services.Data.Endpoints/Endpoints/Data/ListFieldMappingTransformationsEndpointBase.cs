using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Transformations;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the transformations attached to a field mapping, in chain order (FDW-846):
/// <c>GET /field-mappings/{FieldMappingId}/transformations</c>.
/// </summary>
public abstract class ListFieldMappingTransformationsEndpointBase : CrudListEndpointBase<FieldMappingTransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Initializes a new instance of the <see cref="ListFieldMappingTransformationsEndpointBase"/> class.</summary>
    /// <remarks>
    /// The attachments are configuration rows in the transformation store the host declared, so they are read through
    /// that store's configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected ListFieldMappingTransformationsEndpointBase(ILogger<ListFieldMappingTransformationsEndpointBase> logger,
        IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations) : base(logger)
    {
        _configurationGateways = configurationGateways;
        _transformations = transformations;
    }

    /// <inheritdoc/>
    protected override string ResourceName => "field-mapping-transformations";

    /// <inheritdoc/>
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc/>
    protected override string Route => "/field-mappings/{FieldMappingId}/transformations";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List transformations attached to a field mapping";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Returns the transformations applied to a field mapping, in the order they run.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<List<FieldMappingTransformationPayload>>> LoadItems(CancellationToken ct)
    {
        var routeValue = HttpContext.Request.RouteValues["FieldMappingId"]?.ToString();
        if (!Guid.TryParse(routeValue, out var fieldMappingId))
        {
            // Why fail: an empty list here is indistinguishable from a field mapping that genuinely
            // has no transformations, so a mistyped id reads to the caller as a chain that was lost.
            return Task.FromResult(GenericResult<List<FieldMappingTransformationPayload>>.Failure(
                FieldMappingTransformationEndpointLog.FieldMappingIdInvalid(Logger, routeValue ?? string.Empty)));
        }

        return LoadTransforms(fieldMappingId, ct);
    }

    /// <summary>Loads a field mapping's attachments and describes each attached transformation.</summary>
    protected virtual async Task<IGenericResult<List<FieldMappingTransformationPayload>>> LoadTransforms(
        Guid fieldMappingId, CancellationToken ct)
    {
        FieldMappingTransformationEndpointLog.ListingTransforms(Logger);

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<List<FieldMappingTransformationPayload>>();

        var attached = await FieldMappingTransformations
            .ForFieldMapping(gateway.Value!, _transformations.DataStoreName, fieldMappingId, ct)
            .ConfigureAwait(false);
        if (attached.IsFailure)
        {
            FieldMappingTransformationEndpointLog.ListTransformsFailed(Logger, fieldMappingId);
            return attached.ToNewResult<List<FieldMappingTransformationPayload>>();
        }

        // Only the attached transformations are read, each whole for its parameters -- not every
        // transformation there is, to name the few this mapping uses (FDW-951).
        var payloads = new List<FieldMappingTransformationPayload>(attached.Value!.Count);
        foreach (var attachment in attached.Value)
        {
            var read = await _transformations.Get(attachment.TransformationId, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
            if (read.IsFailure)
                return read.ToNewResult<List<FieldMappingTransformationPayload>>();
            if (read.Value is not { } found)
                return GenericResult<List<FieldMappingTransformationPayload>>.Failure(
                    FieldMappingTransformationEndpointLog.TransformationNotFound(Logger, attachment.TransformationId.ToString()));
            if (found is not IFieldTransformationConfiguration transformation)
                return GenericResult<List<FieldMappingTransformationPayload>>.Failure(
                    FieldMappingTransformationEndpointLog.TransformationNotFieldLevel(Logger, found.Name, found.Implementation));

            payloads.Add(new FieldMappingTransformationPayload
            {
                FieldMappingId = fieldMappingId,
                TransformationName = transformation.Name,
                TransformationType = transformation.TransformationType,
                Ordinal = attachment.Ordinal,
                IsSystemProvided = transformation.IsSystemProvided,
                Parameters = TransformationEndpointSupport.ToParameterPayloads(transformation.Parameters)
            });
        }

        FieldMappingTransformationEndpointLog.ListedTransforms(Logger, payloads.Count);
        return GenericResult<List<FieldMappingTransformationPayload>>.Success(payloads);
    }
}
