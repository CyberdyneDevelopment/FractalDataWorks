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
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Lists the transformations (FDW-846): <c>GET /transforms</c>, one page at a time in name order (FDW-951).</summary>
/// <remarks>Each transformation on the page is read whole: its payload carries its parameters.</remarks>
public abstract class ListTransformationsEndpointBase : CrudPageEndpointBase<ITransformationImplementationConfiguration, TransformationPayload>
{
    private readonly TransformationConfigurationProvider _transformations;

    /// <summary>Initializes a new instance of the <see cref="ListTransformationsEndpointBase"/> class.</summary>
    protected ListTransformationsEndpointBase(ILogger<ListTransformationsEndpointBase> logger,
        TransformationConfigurationProvider transformations) : base(logger)
    {
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
    }

    /// <inheritdoc/>
    protected override string ResourceName => "transforms";

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
        FieldMappingTransformEndpointLog.ListingTransformations(Logger);
        return _transformations.Get(paging.Skip, paging.Take, ConfigurationLevels.WholeAggregate, ct);
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<TransformationPayload>> ToSummary(ITransformationImplementationConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<TransformationPayload>.Success(TransformationEndpointSupport.ToPayload(item)));
}
