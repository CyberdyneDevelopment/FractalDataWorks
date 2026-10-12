using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Base endpoint for listing permissions, one page at a time in name order (FDW-951).
/// </summary>
/// <remarks>
/// A page is one read of the permissions domain table: a permission is nothing beyond the fields a list
/// shows, and they all live on the domain row (FDW-1002).
/// </remarks>
public abstract class ListPermissionsEndpointBase : CrudPageEndpointBase<PermissionDomainConfiguration, PermissionSummaryDto>
{
    private readonly IPermissionConfigurationProvider _permissionProvider;
    private readonly ITenantContext? _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="ListPermissionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="permissionProvider">Reads the page of permissions.</param>
    /// <param name="tenantContext">The acting tenant, whose org prefix qualifies each permission's name.</param>
    protected ListPermissionsEndpointBase(ILogger logger, IPermissionConfigurationProvider permissionProvider, ITenantContext? tenantContext = null) : base(logger)
    {
        _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));
        _tenantContext = tenantContext;
    }

    /// <summary>Gets the resource name used for the route: <c>/permissions</c>.</summary>
    protected override string ResourceName => "permissions";

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:read".
    /// </summary>
    protected override string ReadPolicy => "settings/role:read";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<PermissionDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        AuthorizationEndpointLog.ListingPermissions(Logger);
        return _permissionProvider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<PermissionSummaryDto>> ToSummary(PermissionDomainConfiguration item, CancellationToken ct)
    {
        var orgPrefix = _tenantContext?.CurrentTenant?.OrgPrefix;
        return Task.FromResult(GenericResult<PermissionSummaryDto>.Success(
            MapToSummary(item, string.IsNullOrEmpty(orgPrefix) ? null : orgPrefix + ":")));
    }

    /// <summary>Maps a permission, as the permissions domain table holds it, to its summary, qualified by the acting tenant's org prefix when it has one.</summary>
    /// <param name="permission">The permission's domain record.</param>
    /// <param name="orgPrefix">The prefix to put before the permission's name, or none.</param>
    /// <returns>The summary.</returns>
    /// <remarks>
    /// The summary's domain is the permission's OWN domain (for example "users"). It used to be the domain
    /// row's <c>Domain</c>, which is "Permission" for every row, so a console that groups permissions by
    /// domain saw one group; and the category, which the summary declares, was never set.
    /// </remarks>
    protected virtual PermissionSummaryDto MapToSummary(PermissionDomainConfiguration permission, string? orgPrefix)
    {
        return new PermissionSummaryDto
        {
            Id = permission.Id,
            Name = orgPrefix is null ? permission.Name : orgPrefix + permission.Name,
            Domain = permission.PermissionDomain,
            Resource = permission.Resource,
            Action = permission.Action,
            Scope = permission.Scope,
            DisplayName = permission.DisplayName,
            Description = permission.Description,
            Category = permission.Category
        };
    }
}
