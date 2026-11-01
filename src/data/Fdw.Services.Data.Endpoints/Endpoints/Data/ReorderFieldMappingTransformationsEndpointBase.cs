using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Fdw.Data.Abstractions;
using Fdw.Services.Data.Transformations;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reorders the transformations attached to a field mapping (FDW-846):
/// <c>POST /field-mappings/{FieldMappingId}/transformations/reorder</c>.
/// </summary>
/// <remarks>
/// Attachments are child configuration rows (<c>transform.FieldMappingTransformation</c>), so each
/// position change is a <c>ConfigurationSaveCommand</c> through the data gateway, addressed by the
/// attachment's Id. Where they live comes from the Transformation domain provider's registration.
/// </remarks>
public abstract class ReorderFieldMappingTransformationsEndpointBase : Endpoint<ReorderFieldMappingTransformationsRequest>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    /// <summary>Gets the logger instance.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Initializes a new instance of the <see cref="ReorderFieldMappingTransformationsEndpointBase"/> class.</summary>
    /// <remarks>
    /// The attachments are configuration rows in the transformation store the host declared, so they are read and
    /// written through that store's configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected ReorderFieldMappingTransformationsEndpointBase(ILogger logger, IConfigurationGatewayProvider configurationGateways,
        TransformationConfigurationProvider transformations)
    {
        Logger = logger;
        _configurationGateways = configurationGateways;
        _transformations = transformations;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/field-mappings/{FieldMappingId}/transformations/reorder");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Reorder a field mapping's transformations";
            s.Description = "Sets the order a field mapping's attached transformations run in. Name every attached transformation exactly once.";
        });

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, throttling, or tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ReorderFieldMappingTransformationsRequest req, CancellationToken ct)
    {
        try
        {
            var result = await ReorderTransformations(req.FieldMappingId, req.TransformationNames, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
                await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
                return;
            }

            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            await Send.StatusCodeAsync(StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Re-saves each attachment whose position changed. The request must name every attached
    /// transformation exactly once, so a reorder can never silently drop or duplicate one.
    /// </summary>
    protected virtual async Task<IGenericResult> ReorderTransformations(
        Guid fieldMappingId, IList<string> transformationNames, CancellationToken ct)
    {
        FieldMappingTransformationEndpointLog.ReorderingTransforms(Logger, fieldMappingId);

        var gateway = _configurationGateways.Get(_transformations.DataStoreName);
        if (gateway.IsFailure)
            return gateway;

        var attached = await FieldMappingTransformations
            .ForFieldMapping(gateway.Value!, _transformations.DataStoreName, fieldMappingId, ct)
            .ConfigureAwait(false);
        if (attached.IsFailure)
        {
            FieldMappingTransformationEndpointLog.ReorderTransformationsFailed(Logger, fieldMappingId);
            return attached;
        }

        var all = await _transformations.Get(ct).ConfigureAwait(false);
        if (all.IsFailure)
            return all;
        var nameById = all.Value!.ToDictionary(t => t.Id, t => t.Name);

        var byName = new Dictionary<string, FieldMappingTransformationConfiguration>(StringComparer.Ordinal);
        foreach (var attachment in attached.Value!)
        {
            if (nameById.TryGetValue(attachment.TransformationId, out var name))
                byName[name] = attachment;
        }

        var requested = transformationNames.ToHashSet(StringComparer.Ordinal);
        if (requested.Count != transformationNames.Count
            || requested.Count != byName.Count
            || !requested.SetEquals(byName.Keys))
            return GenericResult.Failure(FieldMappingTransformationEndpointLog.ReorderSetMismatch(Logger, fieldMappingId));

        var written = await WriteOrdinals(gateway.Value!, byName, transformationNames, fieldMappingId, ct).ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        FieldMappingTransformationEndpointLog.ReorderedTransforms(Logger, fieldMappingId);
        return GenericResult.Success();
    }

    // Saves each attachment whose position in the requested order differs from the one it holds.
    private async Task<IGenericResult> WriteOrdinals(
        IConfigurationGateway gateway,
        Dictionary<string, FieldMappingTransformationConfiguration> byName,
        IList<string> transformationNames,
        Guid fieldMappingId,
        CancellationToken ct)
    {
        for (var i = 0; i < transformationNames.Count; i++)
        {
            var attachment = byName[transformationNames[i]];
            if (attachment.Ordinal == i)
                continue;

            var saved = await FieldMappingTransformations
                .Save(gateway, _transformations.DataStoreName, new FieldMappingTransformationConfiguration
                {
                    Id = attachment.Id,
                    DataSetFieldMappingId = attachment.DataSetFieldMappingId,
                    TransformationId = attachment.TransformationId,
                    Ordinal = i
                }, ct)
                .ConfigureAwait(false);
            if (saved.IsFailure)
            {
                FieldMappingTransformationEndpointLog.ReorderTransformationsFailed(Logger, fieldMappingId);
                return saved;
            }
        }

        return GenericResult.Success();
    }
}
