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
/// Base for the endpoint that reads one transformation of any kind: what every kind has, then what its own kind adds, a
/// Built transformation's formulas among it.
/// </summary>
public abstract class GetTransformationEndpointBase : CrudGetEndpointBase<TransformationNameRequest, TransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly TransformationKindContext _context;

    /// <summary>Initializes a new instance of the <see cref="GetTransformationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Reads the Transformation domain.</param>
    /// <param name="settings">Supplies the limits on a calculation.</param>
    protected GetTransformationEndpointBase(ILogger<GetTransformationEndpointBase> logger,
        TransformationConfigurationProvider transformations, IEffectiveSettingsProvider settings) : base(logger)
    {
        _transformations = transformations;
        _context = new TransformationKindContext(settings, logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "transforms";

    /// <inheritdoc />
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(TransformationNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<TransformationPayload?>> FindByIdentifier(
        TransformationNameRequest request, CancellationToken ct)
    {
        var found = await _transformations.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<TransformationPayload?>();

        if (found.Value is null)
            return GenericResult<TransformationPayload?>.Success(null);

        var detail = TransformationEndpointSupport.ToDetail(found.Value, _context);
        return detail.IsFailure
            ? detail.ToNewResult<TransformationPayload?>()
            : GenericResult<TransformationPayload?>.Success(detail.Value);
    }
}
