using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Reads who a workspace belongs to: its tenant, and the organization that names it as its default (FDW-1101).
/// </summary>
/// <remarks>
/// A workspace name is unique within a tenant only, so a caller who sees several tenants needs the tenant to tell
/// same-named workspaces apart. The organization is the tenant's default organization when its
/// <c>DefaultWorkspaceId</c> names this workspace; organizations are read through <see cref="IOrganizationProvider"/>,
/// which answers a tenant's default organization, and a tenant's default organization is the only one created today.
/// </remarks>
public static class WorkspaceOwnershipReader
{
    /// <summary>Reads the ownership of <paramref name="workspace"/>.</summary>
    /// <param name="organizations">Reads a tenant's default organization.</param>
    /// <param name="workspace">The workspace.</param>
    /// <param name="logger">The logger that records the read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ownership; a failure when the workspace carries no tenant or its organization could not be read.</returns>
    public static async Task<IGenericResult<WorkspaceOwnership>> Read(
        IOrganizationProvider organizations,
        IWorkspaceImplementationConfiguration workspace,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (organizations is null) throw new ArgumentNullException(nameof(organizations));
        if (workspace is null) throw new ArgumentNullException(nameof(workspace));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        if (workspace.TenantId is not { } tenantId)
            return GenericResult<WorkspaceOwnership>.Failure(WorkspaceOwnershipLog.NoOwningTenant(logger, workspace.Name));

        var organization = await organizations.GetDefault(tenantId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure) return organization.ToNewResult<WorkspaceOwnership>();

        var defaultFor = organization.Value!.DefaultWorkspaceId == workspace.Id ? organization.Value : null;
        WorkspaceOwnershipLog.OwnershipRead(logger, workspace.Name, tenantId.ToString("D", CultureInfo.InvariantCulture), defaultFor is not null);
        return GenericResult<WorkspaceOwnership>.Success(new WorkspaceOwnership(tenantId, defaultFor));
    }
}
