using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Results;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>Lists the owners from the tenant context and the organization and workspace providers (FDW-4100).</summary>
/// <remarks>The providers read under row level security by tenant; this does not filter by the caller's own membership.</remarks>
public sealed class TagOwners : ITagOwners
{
    /// <summary>
    /// The take that asks the organization domain for every organization of the tenant in one page: the largest the provider accepts.
    /// </summary>
    /// <remarks>
    /// The domain provider reads take + 1 rows to know whether a page follows and refuses <see cref="int.MaxValue"/> (ReadArgumentOutOfRange) (FDW-4260).
    /// </remarks>
    public const int EveryOrganization = int.MaxValue - 1;

    private readonly ITenantContext _tenant;
    private readonly IOrganizationConfigurationProvider _organizations;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TagOwners"/> class.</summary>
    /// <param name="tenant">The tenant the caller acts in.</param>
    /// <param name="organizations">The organization provider.</param>
    /// <param name="workspaces">The workspace provider.</param>
    /// <param name="logger">The logger.</param>
    public TagOwners(ITenantContext tenant, IOrganizationConfigurationProvider organizations, IWorkspaceConfigurationProvider workspaces, ILogger<TagOwners>? logger = null)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _logger = logger ?? NullLogger<TagOwners>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<TagOwner>>> List(CancellationToken cancellationToken = default)
    {
        if (_tenant.CurrentTenant is not { } tenant)
        {
            return GenericResult<IReadOnlyList<TagOwner>>.Failure(TagsResultCodes.ByName("TagOwnerTenantUnknown"), _logger, TagDetails.Of());
        }

        // The organization domain keeps its fields on the domain record and its implementation carries none of them, so the composing Get() can
        // never answer for it (ListFieldNotOnImplementation); the domain record page is the path the domain serves (FDW-4233). The take is
        // "all": an owner menu that silently dropped organizations would hide where a tag may be placed.
        var organizationPage = await _organizations.Get(new DomainQuery(null, null, 0, EveryOrganization, false), cancellationToken).ConfigureAwait(false);
        if (organizationPage.IsFailure || organizationPage.Value is not { } page)
        {
            return organizationPage.ToNewResult<IReadOnlyList<TagOwner>>();
        }

        var organizationRows = await page.Items.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (page.Outcome.IsFailure)
        {
            return page.Outcome.ToNewResult<IReadOnlyList<TagOwner>>();
        }

        var workspaces = await _workspaces.Get(cancellationToken).ConfigureAwait(false);
        if (workspaces.IsFailure || workspaces.Value is not { } workspaceRows)
        {
            return workspaces.ToNewResult<IReadOnlyList<TagOwner>>();
        }

        IReadOnlyList<TagOwner> owners =
        [
            new(TagOwnerKinds.Tenant.Name, tenant.Id, tenant.DisplayName),
            .. organizationRows.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new TagOwner(TagOwnerKinds.Organization.Name, row.Id, row.Name)),
            .. workspaceRows.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new TagOwner(TagOwnerKinds.Workspace.Name, row.Id, row.Name)),
        ];

        return GenericResult<IReadOnlyList<TagOwner>>.Success(owners);
    }
}
