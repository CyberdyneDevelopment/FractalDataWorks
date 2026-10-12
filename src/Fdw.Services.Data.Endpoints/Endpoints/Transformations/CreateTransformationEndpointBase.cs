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
/// Base for the endpoint that defines a transformation of any kind: the request's <c>implementation</c> (Field or Built) says
/// which, and that kind checks the request and builds what is stored.
/// </summary>
public abstract class CreateTransformationEndpointBase : CrudCreateEndpointBase<CreateTransformationRequest, TransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly TransformationKindContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateTransformationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Reads and writes the Transformation domain.</param>
    /// <param name="settings">Supplies the limits on a calculation.</param>
    protected CreateTransformationEndpointBase(ILogger<CreateTransformationEndpointBase> logger,
        TransformationConfigurationProvider transformations, IEffectiveSettingsProvider settings) : base(logger)
    {
        _transformations = transformations;
        _context = new TransformationKindContext(settings, logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "transforms";

    /// <inheritdoc />
    protected override string WritePolicy => "datasets:write";

    /// <inheritdoc />
    protected override string GetResourceName(CreateTransformationRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateTransformationRequest request, CancellationToken ct)
    {
        var found = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<bool>();

        return GenericResult<bool>.Success(found.Value is not null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> ValidateCreate(CreateTransformationRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult>(Prepare(request));

    /// <inheritdoc />
    protected override async Task<IGenericResult<TransformationPayload>> Create(CreateTransformationRequest request, CancellationToken ct)
    {
        var prepared = Prepare(request);
        if (prepared.IsFailure)
            return prepared.ToNewResult<TransformationPayload>();

        var saved = await _transformations
            .Save(prepared.Value!, TransformationDomain.Name, request.Implementation, request.Name, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure)
            return saved.ToNewResult<TransformationPayload>();

        var reread = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (reread.IsFailure)
            return reread.ToNewResult<TransformationPayload>();
        if (reread.Value is null)
            return GenericResult<TransformationPayload>.Failure(
                FieldMappingTransformEndpointLog.TransformationNotFound(Logger, request.Name));

        FieldMappingTransformEndpointLog.TransformationSaved(Logger, request.Name, request.Implementation);
        return TransformationEndpointSupport.ToDetail(reread.Value, _context);
    }

    // The kind the request names checks it and builds what is stored.
    private IGenericResult<ITransformationImplementationConfiguration> Prepare(CreateTransformationRequest request)
    {
        var kind = TransformationEndpointSupport.FindKind(request.Implementation, Logger);
        return kind.IsFailure
            ? kind.ToNewResult<ITransformationImplementationConfiguration>()
            : kind.Value!.Prepare(request, _context);
    }
}
