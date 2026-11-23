using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Results;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>Lists the owners from the tenant context and the organization and workspace providers (FDW-4100).</summary>
/// <remarks>The organizations are those of the caller's tenant (the tenant schema has no row level security, so they are narrowed by <c>TenantOrganizations</c>, FDW-4828); the workspaces read under row level security by tenant. This does not filter by the caller's own membership.</remarks>
public sealed class TagOwners : ITagOwners
{
    private readonly ITenantContext _tenant;
    private readonly IOrganizationConfigurationProvider _organizations;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TagOwners"/> class.</summary>
    /// <param name="tenant">The tenant the caller acts in.</param>
    /// <param name="organizations">The organization provider.</param>
    /// <param name="workspaces">The workspace provider.</param>
    /// <param name="settings">The settings the read size of the organizations is stated in.</param>
    /// <param name="logger">The logger.</param>
    public TagOwners(
        ITenantContext tenant,
        IOrganizationConfigurationProvider organizations,
        IWorkspaceConfigurationProvider workspaces,
        IEffectiveSettingsProvider settings,
        ILogger<TagOwners>? logger = null)
    {
        _tenant = tenant;
        _organizations = organizations;
        _workspaces = workspaces;
        _settings = settings;
        _logger = logger ?? NullLogger<TagOwners>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<TagOwner>>> List(CancellationToken cancellationToken = default)
    {
        if (_tenant.CurrentTenant is not { } tenant)
        {
            return GenericResult<IReadOnlyList<TagOwner>>.Failure(TagsResultCodes.ByName("TagOwnerTenantUnknown"), _logger, TagDetails.Of());
        }

        // The organization domain keeps its fields on the domain record and its implementation carries none of them, so the composing Get() can never answer for it
        // (ListFieldNotOnImplementation); the domain record page is the path the domain serves (FDW-4233). The tenant schema has no row-level security, so the read is narrowed to the
        // caller's tenant by the one mechanism every organization list uses, TenantOrganizations (FDW-4828), with the read size from the setting TagOwnerReadSize (FDW-6737): an owner menu
        // that silently dropped organizations would hide where a tag may be placed, so a tenant with more organizations than the size fails instead.
        var organizationRows = await _organizations.OfTenant(tenant.Id, _settings, SettingDefinitions.TagOwnerReadSize, _logger, cancellationToken).ConfigureAwait(false);
        if (organizationRows.IsFailure || organizationRows.Value is not { } organizationList)
            return organizationRows.ToNewResult<IReadOnlyList<TagOwner>>();

        var workspaces = await _workspaces.Get(cancellationToken).ConfigureAwait(false);
        if (workspaces.IsFailure || workspaces.Value is not { } workspaceRows)
        {
            return workspaces.ToNewResult<IReadOnlyList<TagOwner>>();
        }

        IReadOnlyList<TagOwner> owners =
        [
            new(TagOwnerKinds.Tenant.Name, tenant.Id, tenant.DisplayName),
            .. organizationList.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new TagOwner(TagOwnerKinds.Organization.Name, row.Id, row.Name)),
            .. workspaceRows.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new TagOwner(TagOwnerKinds.Workspace.Name, row.Id, row.Name)),
        ];

        return GenericResult<IReadOnlyList<TagOwner>>.Success(owners);
    }
}
