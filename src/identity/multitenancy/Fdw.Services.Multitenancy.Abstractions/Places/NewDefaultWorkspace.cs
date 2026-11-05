using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make the default workspace of an organization (FDW-3668): the workspace the organization's row already names in
/// its <c>DefaultWorkspaceId</c>, owned by the person who founds it.
/// </summary>
/// <param name="WorkspaceId">The id the organization names as its default workspace; the workspace is made with it.</param>
/// <param name="TenantId">The tenant the organization, and so the workspace, belongs to; it may not be the tenant the caller is acting in.</param>
/// <param name="TenantSlug">The tenant's slug, which the workspace's name starts with.</param>
/// <param name="OrganizationId">The organization the workspace is the default of.</param>
/// <param name="OrganizationName">The organization's display name.</param>
/// <param name="OrganizationSlug">The organization's slug, which names the workspace of an organization that is not the tenant's default.</param>
/// <param name="OrganizationIsDefault">Whether the organization is its tenant's default organization.</param>
/// <param name="OwnerUserId">The person who owns the workspace: the administrator who founds the tenant or the organization.</param>
public sealed record NewDefaultWorkspace(
    Guid WorkspaceId,
    Guid TenantId,
    string TenantSlug,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    bool OrganizationIsDefault,
    Guid OwnerUserId);
