using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Base endpoint for listing roles, one page at a time in name order (FDW-951).
/// </summary>
/// <remarks>
/// A page is one read of the roles domain table: the fields a list shows live on the domain row, so no
/// role's implementation and none of its permissions are read to list it (FDW-1002).
/// </remarks>
public abstract class ListRolesEndpointBase : CrudPageEndpointBase<RoleDomainConfiguration, RoleSummaryResponse>
{
    private readonly IRoleConfigurationProvider _roleProvider;

    /// <summary>Initializes a new instance of the <see cref="ListRolesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleProvider">Reads the page of roles.</param>
    protected ListRolesEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider) : base(logger)
    {
        _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
    }

    /// <summary>Gets the resource name used for the route: <c>/roles</c>.</summary>
    protected override string ResourceName => "roles";

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:read".
    /// </summary>
    protected override string ReadPolicy => "settings/role:read";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<RoleDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _roleProvider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<RoleSummaryResponse>> ToSummary(RoleDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<RoleSummaryResponse>.Success(MapToSummary(item)));

    /// <summary>Maps a role, as the roles domain table holds it, to its summary.</summary>
    /// <param name="role">The role's domain record.</param>
    /// <returns>The summary.</returns>
    protected virtual RoleSummaryResponse MapToSummary(RoleDomainConfiguration role)
    {
        return new RoleSummaryResponse
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsTenantScoped = role.IsTenantScoped,
            CreatedAt = role.CreatedAt
        };
    }
}
