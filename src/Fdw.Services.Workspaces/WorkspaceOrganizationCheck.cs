using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Checks that the organization a workspace is put in is one of the workspace's own tenant (FDW-2338).
/// </summary>
/// <remarks>
/// The workspace's place in the scope tree (tenant, organization, workspace) is the organization it names, or none when
/// it sits directly under its tenant. An organization that does not exist and one of another tenant are refused with
/// the same reason, so the refusal does not tell a caller which organizations other tenants hold.
/// </remarks>
public static class WorkspaceOrganizationCheck
{
    /// <summary>Checks <paramref name="organizationId"/> against the tenant of the workspace.</summary>
    /// <param name="organizations">Reads an organization.</param>
    /// <param name="workspaceName">The workspace being saved, for the refusal.</param>
    /// <param name="organizationId">The organization the workspace is to be in.</param>
    /// <param name="tenantId">The tenant the workspace is in; null when it is not known.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the organization is one of the tenant's; the refusal when not; the read's failure when it could not be answered.</returns>
    public static async Task<IGenericResult> Check(
        IOrganizationProvider organizations,
        string workspaceName,
        Guid organizationId,
        Guid? tenantId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        ArgumentNullException.ThrowIfNull(logger);

        if (tenantId is not { } tenant)
            return Refuse(workspaceName, organizationId, "cannot be checked because the workspace's tenant is not known", logger);

        // An organization the store does not hold answers as missing, which is the same refusal as one of another tenant.
        var read = await organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure && !FailureCategory.IsMissing(read))
            return read;

        return read.IsSuccess && read.Value is { } organization && organization.TenantId == tenant && !organization.IsDeleted
            ? GenericResult.Success()
            : Refuse(workspaceName, organizationId, "is not an organization of this workspace's tenant", logger);
    }

    private static IGenericResult Refuse(string workspaceName, Guid organizationId, string reason, ILogger logger) =>
        GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceOrganizationInvalid"), logger,
            ResultDetails.Create("name", workspaceName, "value", organizationId.ToString("D", CultureInfo.InvariantCulture), "reason", reason));
}
