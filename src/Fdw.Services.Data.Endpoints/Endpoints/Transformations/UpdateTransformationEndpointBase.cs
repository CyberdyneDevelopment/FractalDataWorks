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
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base for the endpoint that changes a transformation of any kind. The stored transformation's own kind checks the change
/// and builds what replaces it; a member the request leaves out keeps its stored value.
/// </summary>
public abstract class UpdateTransformationEndpointBase : CrudUpdateEndpointBase<UpdateTransformationRequest, TransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly TransformationKindContext _context;

    /// <summary>Initializes a new instance of the <see cref="UpdateTransformationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Reads and writes the Transformation domain.</param>
    /// <param name="settings">Supplies the limits on a calculation.</param>
    protected UpdateTransformationEndpointBase(ILogger<UpdateTransformationEndpointBase> logger,
        TransformationConfigurationProvider transformations, IEffectiveSettingsProvider settings) : base(logger)
    {
        _transformations = transformations;
        _context = new TransformationKindContext(settings, logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "transformations";

    /// <inheritdoc />
    protected override string WritePolicy => "datasets:write";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateTransformationRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<TransformationPayload?>> FindForUpdate(
        UpdateTransformationRequest request, CancellationToken ct)
    {
        var found = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<TransformationPayload?>();

        return GenericResult<TransformationPayload?>.Success(
            found.Value is null ? null : TransformationEndpointSupport.ToPayload(found.Value));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> ValidateUpdate(
        UpdateTransformationRequest request, TransformationPayload existing, CancellationToken ct)
    {
        if (existing.IsSystemProvided)
            return GenericResult.Failure(
                FieldMappingTransformationEndpointLog.SystemProvidedTransformationImmutable(Logger, existing.Name));

        var current = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
            return GenericResult.Failure(FieldMappingTransformationEndpointLog.TransformationNotFound(Logger, request.Name));

        return Prepare(request, current.Value);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<TransformationPayload>> Update(
        UpdateTransformationRequest request, TransformationPayload existing, CancellationToken ct)
    {
        var current = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
            return GenericResult<TransformationPayload>.Failure(
                FieldMappingTransformationEndpointLog.TransformationNotFound(Logger, request.Name));

        var prepared = Prepare(request, current.Value);
        if (prepared.IsFailure)
            return prepared.ToNewResult<TransformationPayload>();

        var saved = await _transformations
            .Update(prepared.Value!, TransformationDomain.Name, current.Value.Implementation, request.Name, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure)
            return saved.ToNewResult<TransformationPayload>();

        var reread = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (reread.IsFailure)
            return reread.ToNewResult<TransformationPayload>();
        if (reread.Value is null)
            return GenericResult<TransformationPayload>.Failure(
                FieldMappingTransformationEndpointLog.TransformationNotFound(Logger, request.Name));

        FieldMappingTransformationEndpointLog.TransformationSaved(Logger, request.Name, current.Value.Implementation);
        return TransformationEndpointSupport.ToDetail(reread.Value, _context);
    }

    // The stored transformation's own kind checks the change and builds what replaces it.
    private IGenericResult<ITransformationImplementationConfiguration> Prepare(
        UpdateTransformationRequest request, ITransformationImplementationConfiguration current)
    {
        var kind = TransformationEndpointSupport.FindKind(current.Implementation, Logger);
        return kind.IsFailure
            ? kind.ToNewResult<ITransformationImplementationConfiguration>()
            : kind.Value!.PrepareUpdate(request, current, _context);
    }
}
