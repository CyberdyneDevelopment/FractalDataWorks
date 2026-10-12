using System;
using System.Globalization;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The names of the permissions that belong to one workspace.
/// </summary>
/// <remarks>
/// <para>
/// <c>workspaces/{id}:view</c> and <c>workspaces/{id}:query</c>, created with the workspace and removed
/// with it (FDW-937), and read by <see cref="WorkspaceAccessPolicy"/> (FDW-936). One place spells them
/// so the permission that is written and the permission that is checked cannot drift.
/// </para>
/// <para>
/// Keyed by the workspace's Id rather than its name. Workspace names are unique within a tenant;
/// permission names are unique across every tenant. A name-keyed permission would refuse the second
/// tenant to pick a name, reveal that another tenant already has it, and on a global administrator's
/// delete could match another tenant's permission. The Id has none of those problems, is fixed length,
/// and never changes. The readable name travels in the permission's display name.
/// </para>
/// <para>
/// A slash, not a colon, between <c>workspaces</c> and the Id: every permission name is one
/// <c>resource:action</c> pair, and a sub-resource is spelled with a slash as the seeded ones are
/// (<c>settings/tenant:read</c>). A third colon-separated part reads as a tenant's org prefix to the
/// role-permission endpoint, which would refuse the grant for a tenant that has one.
/// </para>
/// </remarks>
public static class WorkspacePermissionNames
{
    /// <summary>The action that lets a caller view one workspace.</summary>
    public const string ViewAction = "view";

    /// <summary>The action that lets a caller query one workspace's data sets.</summary>
    public const string QueryAction = "query";

    /// <summary>
    /// The platform permission that decides the membership of every workspace in the tenant: add,
    /// change and remove members, and approve or deny requests to join (FDW-1071). Not
    /// <c>workspaces:write</c>, which changes a workspace without deciding who belongs to it.
    /// </summary>
    public const string ManageMembers = "workspaces:members";

    /// <summary>Gets the resource part of a workspace's own permissions: <c>workspaces/{id}</c>, the Id lower-case.</summary>
    /// <param name="workspaceId">The workspace's durable Id.</param>
    /// <returns>The resource.</returns>
    public static string Resource(Guid workspaceId)
        => string.Create(CultureInfo.InvariantCulture, $"workspaces/{workspaceId:D}");

    /// <summary>Gets <c>workspaces/{id}:{action}</c>.</summary>
    /// <param name="workspaceId">The workspace's durable Id.</param>
    /// <param name="action"><see cref="ViewAction"/> or <see cref="QueryAction"/>.</param>
    /// <returns>The permission name.</returns>
    public static string For(Guid workspaceId, string action) => $"{Resource(workspaceId)}:{action}";
}
