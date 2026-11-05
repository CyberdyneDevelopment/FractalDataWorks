using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
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

        var organizations = await _organizations.Get(cancellationToken).ConfigureAwait(false);
        if (organizations.IsFailure || organizations.Value is not { } organizationRows)
        {
            return organizations.ToNewResult<IReadOnlyList<TagOwner>>();
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
