using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Base endpoint for listing the organizations of the caller's tenant, one page at a time: <c>GET /organizations</c>,
/// under <c>organizations:read</c> (FDW-1010, FDW-1016).
/// </summary>
/// <remarks>
/// The page is one read of the organization domain table. ConfigurationDb has no row-level policy on the
/// tenant schema, so the endpoint scopes the read to the caller's tenant itself, filtering on the
/// <c>tenantId</c> claim of the token. A token with no tenant is refused with its own failure rather than
/// answered with an empty page: "your tenant has no organizations" and "you have no tenant" are
/// different facts.
/// </remarks>
public abstract class ListOrganizationsEndpointBase : CrudPageEndpointBase<OrganizationDomainConfiguration, OrganizationSummaryResponse>
{
    private readonly IOrganizationConfigurationProvider _organizations;

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="organizations">Reads the page of organizations.</param>
    protected ListOrganizationsEndpointBase(ILogger logger, IOrganizationConfigurationProvider organizations) : base(logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/organizations</c>, <c>organizations:read</c>.</summary>
    protected override string ResourceName => "organizations";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<OrganizationDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            return GenericResult.Failure(
                    AuthenticationResultCodes.ByName("CallerHasNoTenant"), Logger,
                    ResultDetails.Create("operation", "list organizations"))
                .ToNewResult<ConfigurationPage<OrganizationDomainConfiguration>>();
        }

        OrganizationEndpointLog.ListingOrganizations(Logger, tenantId.ToString());

        return await _organizations.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(OrganizationDomainConfiguration.TenantId),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = tenantId,
                    },
                },
                null, paging.Skip, paging.Take, false),
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<OrganizationSummaryResponse>> ToSummary(
        OrganizationDomainConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<OrganizationSummaryResponse>.Success(new OrganizationSummaryResponse
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            IsDefault = item.IsDefault,
            IsAdminOrg = item.IsAdminOrg,
            IsActive = item.IsActive,
            DefaultWorkspaceId = item.DefaultWorkspaceId,
        }));
}
