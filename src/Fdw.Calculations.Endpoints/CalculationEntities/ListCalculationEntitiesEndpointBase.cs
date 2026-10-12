using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for listing calculation entities, one page at a time.
/// Route: GET /calculation-entities
/// </summary>
/// <remarks>
/// A page is one read of the calculation entities domain table: a summary is the calculation's id, name,
/// description, kind (the domain row's implementation) and whether it is enabled, all on the domain row, so no
/// calculation is composed to list it (FDW-1006). It streams the page envelope <c>{skip, take, hasMore, items}</c>
/// every domain list uses; it used to compose every calculation and answer an in-memory <c>PaginatedResponse</c>
/// (items and totalCount).
/// </remarks>
public abstract class ListCalculationEntitiesEndpointBase : CrudPageEndpointBase<CalculationEntityDomainConfiguration, CalculationEntitySummaryDto>
{
    private readonly ICalculationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListCalculationEntitiesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the page from the calculation entities domain table.</param>
    protected ListCalculationEntitiesEndpointBase(ILogger<ListCalculationEntitiesEndpointBase> logger, ICalculationConfigurationProvider provider) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List all calculation entities";

    /// <inheritdoc/>
    protected override string EndpointDescription => "Streams one page of the configured calculation entities in name order.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <summary>Produces the summary written for one calculation entity; one with no type fails the page.</summary>
    /// <param name="item">The calculation entity, as the domain table holds it.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary, or the failure that ends the page.</returns>
    /// <remarks>
    /// A calculation is always one of a type (Formula, Windowed), which the summary declares required. A row
    /// with none is a fault in what was persisted: the page ends with that failure, as the previous list, which
    /// composed every entity, failed on it too. It is not dropped from the list and given no made-up type.
    /// </remarks>
    protected override Task<IGenericResult<CalculationEntitySummaryDto>> ToSummary(CalculationEntityDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(string.IsNullOrEmpty(item.Implementation)
            ? GenericResult<CalculationEntitySummaryDto>.Failure(CalculationEndpointLog.CalculationEntityMissingType(Logger, item.Name))
            : GenericResult<CalculationEntitySummaryDto>.Success(new CalculationEntitySummaryDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                CalculationEntityType = item.Implementation!,
                IsEnabled = item.IsEnabled,
            }));
}
