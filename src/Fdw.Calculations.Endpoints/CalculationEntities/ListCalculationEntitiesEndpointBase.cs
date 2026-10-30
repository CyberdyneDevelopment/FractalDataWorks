using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Logging;
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
/// <para>
/// Each calculation carries <c>versionCount</c>: how many versions of it the caller may read, the calculation itself counting as one. The versions of
/// every calculation on the page are read in one query, however many the page holds (FDW-2560).
/// </para>
/// </remarks>
public abstract class ListCalculationEntitiesEndpointBase : CrudPageEndpointBase<CalculationEntityDomainConfiguration, CalculationEntitySummaryDto>
{
    private readonly ICalculationConfigurationProvider _provider;
    private readonly ICalculationVersionConfigurationProvider _versions;
    private readonly IPlaceAncestry _ancestry;
    private readonly IScopeAuthority _authority;
    private readonly IAuthenticationContextAccessor _authContext;
    private IReadOnlyDictionary<Guid, int>? _versionCounts;

    /// <summary>Initializes a new instance of the <see cref="ListCalculationEntitiesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the page from the calculation entities domain table.</param>
    /// <param name="versions">Reads the versions of calculations held at organizations and workspaces.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    protected ListCalculationEntitiesEndpointBase(
        ILogger<ListCalculationEntitiesEndpointBase> logger,
        ICalculationConfigurationProvider provider,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _versions = versions ?? throw new ArgumentNullException(nameof(versions));
        _ancestry = ancestry ?? throw new ArgumentNullException(nameof(ancestry));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List all calculation entities";

    /// <inheritdoc/>
    protected override string EndpointDescription => "Streams one page of the configured calculation entities in name order.";

    /// <inheritdoc/>
    protected override async Task<IGenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var page = await _provider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct).ConfigureAwait(false);
        if (page.IsFailure || page.Value is not { } read)
        {
            return page;
        }

        // The page is at most one page of calculations, so it is read through here to count the versions of all of them in one query.
        var calculations = new List<CalculationEntityDomainConfiguration>();
        await foreach (var calculation in read.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            calculations.Add(calculation);
        }

        if (read.Outcome.IsFailure)
        {
            return read.Outcome.ToNewResult<ConfigurationPage<CalculationEntityDomainConfiguration>>();
        }

        if (CallerPlaces.Of(_authContext, null) is not { } origin)
        {
            return GenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>.Failure(PlaceAncestryLog.CallerHasNoPlace(Logger));
        }

        var counts = await PlaceVersions.Counts(
            CalculationVersionSource.Of(_versions),
            _ancestry,
            _authority,
            calculations.Select(calculation => calculation.Id).ToList(),
            origin,
            ReadPolicy,
            Logger,
            ct).ConfigureAwait(false);
        if (counts.IsFailure || counts.Value is not { } counted)
        {
            return counts.ToNewResult<ConfigurationPage<CalculationEntityDomainConfiguration>>();
        }

        _versionCounts = counted;
        return GenericResult<ConfigurationPage<CalculationEntityDomainConfiguration>>.Success(
            new ConfigurationPage<CalculationEntityDomainConfiguration>(read.Skip, read.Take, read.HasMore, _ => calculations.ToAsyncEnumerable(), read.Total));
    }

    /// <summary>Produces the summary written for one calculation entity; one with no type fails the page.</summary>
    /// <param name="item">The calculation entity, as the domain table holds it.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary, or the failure that ends the page.</returns>
    /// <remarks>
    /// A calculation is always one of a type (Windowed), which the summary declares required. A row
    /// with none is a fault in what was persisted: the page ends with that failure, as the previous list, which
    /// composed every entity, failed on it too. It is not dropped from the list and given no made-up type.
    /// </remarks>
    protected override Task<IGenericResult<CalculationEntitySummaryDto>> ToSummary(CalculationEntityDomainConfiguration item, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(item.Implementation))
        {
            return Task.FromResult<IGenericResult<CalculationEntitySummaryDto>>(
                GenericResult<CalculationEntitySummaryDto>.Failure(CalculationEndpointLog.CalculationEntityMissingType(Logger, item.Name)));
        }

        var counts = _versionCounts ?? throw new InvalidOperationException("The versions of the page's calculations are counted before the page is loaded");
        return Task.FromResult<IGenericResult<CalculationEntitySummaryDto>>(counts.TryGetValue(item.Id, out var count)
            ? GenericResult<CalculationEntitySummaryDto>.Success(new CalculationEntitySummaryDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                CalculationEntityType = item.Implementation!,
                IsEnabled = item.IsEnabled,
                VersionCount = count,
            })
            : GenericResult<CalculationEntitySummaryDto>.Failure(CalculationVersionLog.CalculationNotFound(Logger, item.Id.ToString("D"))));
    }
}
