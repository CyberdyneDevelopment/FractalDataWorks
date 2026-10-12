using Fdw.Services.Identity;
﻿using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Lists the managed identities configured in this deployment.
/// </summary>
/// <remarks>
/// Returns no credential and no token — see <see cref="IdentitySummaryResponse"/>. A page is one read of the
/// identities domain table: a summary is the identity's id, its name and its mechanism (the domain row's
/// implementation), so no identity is composed to list it (FDW-1006). It streams the page envelope
/// <c>{skip, take, hasMore, items}</c> every domain list uses; it used to compose every identity and answer an
/// in-memory <c>PaginatedResponse</c> (items and totalCount).
/// </remarks>
public abstract class ListIdentitiesEndpointBase : CrudPageEndpointBase<DomainConfiguration, IdentitySummaryResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListIdentitiesEndpointBase"/> class.</summary>
    protected ListIdentitiesEndpointBase(ILogger<ListIdentitiesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the configuration provider a page of identities is read through.</summary>
    protected abstract IdentityServiceConfigurationProvider Identities { get; }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "identities";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "identities:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List configured managed identities";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Streams one page of the configured managed identities in name order. Never returns credentials or tokens.";

    /// <summary>Reads one page of identities from the identities domain table.</summary>
    /// <param name="paging">The page the caller asked for.</param>
    /// <param name="ct">Propagated cancellation token.</param>
    /// <returns>The page, or a structured failure when its rows could not be read.</returns>
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => Identities.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <summary>Produces the summary written for one identity.</summary>
    /// <param name="item">The identity, as the identities domain table holds it.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary.</returns>
    protected override Task<IGenericResult<IdentitySummaryResponse>> ToSummary(DomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<IdentitySummaryResponse>.Success(MapToSummary(item)));

    /// <summary>Maps a domain row to its summary view.</summary>
    /// <param name="configuration">The identity's domain row.</param>
    /// <returns>The summary.</returns>
    protected virtual IdentitySummaryResponse MapToSummary(DomainConfiguration configuration)
        => new()
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Mechanism = configuration.Implementation,
        };
}
