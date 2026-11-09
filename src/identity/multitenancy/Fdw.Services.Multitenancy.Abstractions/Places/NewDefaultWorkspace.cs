using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What is needed to make the default workspace of an organization (FDW-3668): the workspace the organization's row already names in
/// its <c>DefaultWorkspaceId</c>, owned by the person who founds it. The workspace is named after the thing that owns it (FDW-4186).
/// </summary>
/// <param name="WorkspaceId">The id the organization names as its default workspace; the workspace is made with it.</param>
/// <param name="TenantId">The tenant the organization, and so the workspace, belongs to; it may not be the tenant the caller is acting in.</param>
/// <param name="OwnerName">The name the workspace takes, exactly: the tenant's display name for the tenant's default organization, the organization's name for any other.</param>
/// <param name="OrganizationId">The organization the workspace is the default of.</param>
/// <param name="OwnerUserId">The person who owns the workspace: the administrator who founds the tenant or the organization.</param>
public sealed record NewDefaultWorkspace(
    Guid WorkspaceId,
    Guid TenantId,
    string OwnerName,
    Guid OrganizationId,
    Guid OwnerUserId);
