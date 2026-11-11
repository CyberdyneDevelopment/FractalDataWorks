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
/// Reads who a workspace belongs to: its tenant, and the organization that names it as its default (FDW-1101, FDW-3668).
/// </summary>
/// <remarks>
/// A workspace name is unique within a tenant only, so a caller who sees several tenants needs the tenant to tell
/// same-named workspaces apart. The workspace's own organization is read through <see cref="IOrganizationProvider"/>, and the
/// workspace is that organization's default workspace when its <c>DefaultWorkspaceId</c> names this workspace. A workspace always
/// has an organization; one that names none is a failure, never a workspace under its tenant.
/// </remarks>
public static class WorkspaceOwnershipReader
{
    /// <summary>Reads the ownership of <paramref name="workspace"/>.</summary>
    /// <param name="organizations">Reads the workspace's organization.</param>
    /// <param name="workspace">The workspace.</param>
    /// <param name="logger">The logger that records the read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ownership; a failure when the workspace carries no tenant or no organization, or its organization could not be read.</returns>
    public static async Task<IGenericResult<WorkspaceOwnership>> Read(
        IOrganizationProvider organizations,
        IWorkspaceImplementationConfiguration workspace,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (workspace.TenantId is not { } tenantId)
            return GenericResult<WorkspaceOwnership>.Failure(WorkspaceOwnershipLog.NoOwningTenant(logger, workspace.Name));

        if (workspace.OrganizationId == Guid.Empty)
            return GenericResult<WorkspaceOwnership>.Failure(WorkspaceOwnershipLog.NoOwningOrganization(logger, workspace.Name));

        var organization = await organizations.Get(workspace.OrganizationId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure) return organization.ToNewResult<WorkspaceOwnership>();

        var defaultFor = organization.Value!.DefaultWorkspaceId == workspace.Id ? organization.Value : null;
        WorkspaceOwnershipLog.OwnershipRead(logger, workspace.Name, tenantId.ToString("D", CultureInfo.InvariantCulture), defaultFor is not null);
        return GenericResult<WorkspaceOwnership>.Success(new WorkspaceOwnership(tenantId, defaultFor));
    }
}
