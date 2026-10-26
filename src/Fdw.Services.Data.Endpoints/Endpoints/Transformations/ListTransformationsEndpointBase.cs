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
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Lists the transformations (FDW-846): <c>GET /transformations</c>, one page at a time in name order (FDW-951).</summary>
/// <remarks>Each transformation on the page is read whole: its payload carries its parameters, and each result of a Built transformation its value type.</remarks>
public abstract class ListTransformationsEndpointBase : CrudPageEndpointBase<ITransformationImplementationConfiguration, TransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;
    private readonly TransformationKindContext _context;

    /// <summary>Initializes a new instance of the <see cref="ListTransformationsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transformations">Reads the Transformation domain.</param>
    /// <param name="settings">Supplies the limits on a calculation, which typing a result needs.</param>
    protected ListTransformationsEndpointBase(ILogger<ListTransformationsEndpointBase> logger,
        TransformationConfigurationProvider transformations, IEffectiveSettingsProvider settings) : base(logger)
    {
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        _context = new TransformationKindContext(settings ?? throw new ArgumentNullException(nameof(settings)), logger);
    }

    /// <inheritdoc/>
    protected override string ResourceName => "transformations";

    /// <inheritdoc/>
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List transformations";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Streams one page of the transformations defined once and attachable to field mappings, standard (system-provided) and custom, in name order.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<ConfigurationPage<ITransformationImplementationConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        FieldMappingTransformationEndpointLog.ListingTransformations(Logger);
        return _transformations.Get(paging.Skip, paging.Take, ConfigurationLevels.WholeAggregate, ct);
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<TransformationPayload>> ToSummary(ITransformationImplementationConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<TransformationPayload>.Success(TransformationEndpointSupport.ToSummary(item, _context)));
}
