using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Who a workspace belongs to: the tenant that owns it, and the organization that names it as its default, if one does.
/// </summary>
/// <param name="TenantId">The tenant that owns the workspace.</param>
/// <param name="DefaultFor">
/// The organization whose <c>DefaultWorkspaceId</c> names this workspace; <see langword="null"/> when none does.
/// </param>
public sealed record WorkspaceOwnership(System.Guid TenantId, OrganizationConfiguration? DefaultFor);
